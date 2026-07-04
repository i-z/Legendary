using Legendary.Data.Context;
using Legendary.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Legendary.Tests;

public class DataModelTests
{
    private static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void TrashEntry_SetsSoftDeleteAndUpdatesCount()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var user = new User
        {
            usertype = 0,
            name = "u1",
            email = "u1@test.local",
            password = "x",
            active = true,
            datecreated = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();

        var book = new Book { userId = user.userId, title = "Book A", favorite = false, sort = 1 };
        context.Books.Add(book);
        context.SaveChanges();

        var entry = new Entry
        {
            userId = user.userId,
            bookId = book.bookId,
            chapter = 0,
            sort = 1,
            datecreated = DateTime.UtcNow,
            datemodified = DateTime.UtcNow,
            title = "Entry A",
            summary = "S",
            isTrashed = false
        };
        context.Entries.Add(entry);
        context.SaveChanges();

        var model = new EntryModel(context);
        var count = model.TrashEntry(user.userId, entry.entryId);

        Assert.Equal(1, count);
        Assert.True(context.Entries.Single(e => e.entryId == entry.entryId).isTrashed);
        Assert.Empty(model.GetList(user.userId, book.bookId));
    }

    [Fact]
    public void TrashBook_HidesBookFromActiveList()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var user = new User
        {
            usertype = 0,
            name = "u1",
            email = "u1@test.local",
            password = "x",
            active = true,
            datecreated = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();

        var model = new BookModel(context);
        var bookId = model.CreateBook(user.userId, "Book A", false, 1);

        model.TrashBook(user.userId, bookId);

        Assert.Empty(model.GetList(user.userId));
        Assert.True(context.Books.Single(b => b.bookId == bookId).isTrashed);
    }

    [Fact]
    public void TrashGetList_DoesNotLeakTrashedChaptersFromOtherUsers()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var user1 = new User
        {
            usertype = 0,
            name = "u1",
            email = "u1@test.local",
            password = "x",
            active = true,
            datecreated = DateTime.UtcNow
        };
        var user2 = new User
        {
            usertype = 0,
            name = "u2",
            email = "u2@test.local",
            password = "x",
            active = true,
            datecreated = DateTime.UtcNow
        };
        context.Users.AddRange(user1, user2);
        context.SaveChanges();

        var user1Book = new Book { userId = user1.userId, title = "U1 Book", isTrashed = true };
        var user2Book = new Book { userId = user2.userId, title = "U2 Book", isTrashed = false };
        context.Books.AddRange(user1Book, user2Book);
        context.SaveChanges();

        context.Chapters.AddRange(
            new Chapter { bookId = user1Book.bookId, chapter = 1, title = "U1 Chapter", isTrashed = true },
            new Chapter { bookId = user2Book.bookId, chapter = 1, title = "U2 Chapter", isTrashed = true }
        );
        context.SaveChanges();

        var trash = new TrashModel(context);
        var result = trash.GetList(user1.userId);

        Assert.Single(result.Item2);
        Assert.Equal(user1Book.bookId, result.Item2[0].bookId);
        Assert.DoesNotContain(result.Item2, c => c.bookId == user2Book.bookId);
    }
}

