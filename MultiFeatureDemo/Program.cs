﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MultiFeatureDemo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (var context = new BlogContext())
            {
                // Drop database for clean state
                context.Database.EnsureDeleted();

                if (context.Database.EnsureCreated())
                {
                    context.AddRange(
                        CreateAuthorBlogPost1(),
                        CreateAuthorBlogPost2(),
                        CreateAuthorBlogPost3()
                    );
                    context.SaveChanges();
                }
            }

            using (var context = new BlogContext(logCommand: true))
            {
                var allBlogsWithPosts = context.Blogs.Include(b => b.Posts);

                foreach (var blog in allBlogsWithPosts)
                {
                    Console.WriteLine(blog.Name);
                    Console.WriteLine($" - {blog.Posts.Count}");
                }
            }

            Console.WriteLine("Program finished!");
        }

        private static Author CreateAuthorBlogPost1() => new Author
        {
            Name = "Diego",
            Blogs =
                {
                    new Blog
                    {
                        Name = ".NET Blog",
                        Url = "https://blogs.msdn.microsoft.com/dotnet",
                        Posts =
                        {
                            new Post
                            {
                                Title = "Announcing Entity Framework Core 3.0",
                                PublishDate = new DateTime(2019, 9, 12)
                            },
                            new Post
                            {
                                Title = "Announcing Entity Framework Core 2.2",
                                PublishDate = new DateTime(2018, 12, 4)
                            }
                        }
                    }
                }
        };

        private static Author CreateAuthorBlogPost2() => new Author
        {
            Name = "Brice",
            Blogs =
                {
                    new Blog
                        {
                            Name = "Brice's Blog",
                            Url = "https://www.bricelam.net/",
                            Posts =
                            {
                                new Post
                                {
                                    Title = "Microsoft.Data.Sqlite 3.0",
                                    PublishDate = new DateTime(2019, 9, 18)
                                },
                                new Post
                                {
                                    Title = "Announcing Microsoft.Data.Sqlite 2.1",
                                    PublishDate = new DateTime(2018, 5, 24)
                                }
                            }
                        }
                    }
        };

        private static Author CreateAuthorBlogPost3() => new Author
        {
            Name = "Arthur",
            Blogs =
                {
                    new Blog
                    {
                        Name = "One Unicorn...",
                        Posts =
                        {
                            new Post
                            {
                                Title = "Magic Leap One is everything I hoped it would be",
                                PublishDate = new DateTime(2019, 9, 4)
                            }
                        }
                    }
                }
        };
    }

    public class BlogContext : DbContext
    {
        private readonly bool _logCommand;

        private static ILoggerFactory ContextLoggerFactory => new ConsoleLoggerFactory();

        public BlogContext(bool logCommand = false) => _logCommand = logCommand;

        // Declare DBSets
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Select 1 provider
            optionsBuilder
                .UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=BloggingDemo;Trusted_Connection=True;Connect Timeout=5;ConnectRetryCount=0")
                .EnableSensitiveDataLogging();

            if (_logCommand)
            {
                optionsBuilder.UseLoggerFactory(ContextLoggerFactory);
            }

            optionsBuilder.AddInterceptors(new MyCommandInterceptor());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure model
        }

        private class ConsoleLoggerFactory : ILoggerFactory
        {
            private readonly SqlLogger _logger;
            public ConsoleLoggerFactory()
            {
                _logger = new SqlLogger();
            }

            public void AddProvider(ILoggerProvider provider)
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return _logger;
            }

            public void Dispose()
            {
            }

            private class SqlLogger : ILogger
            {
                public IDisposable BeginScope<TState>(TState state)
                {
                    return null;
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return true;
                }

                public void Log<TState>(
                    LogLevel logLevel,
                    EventId eventId,
                    TState state,
                    Exception exception,
                    Func<TState, Exception, string> formatter
                )
                {
                    if (eventId == RelationalEventId.CommandExecuted)
                    {
                        var message = formatter(state, exception)?.Trim();
                        Console.WriteLine(message + Environment.NewLine);
                    }
                }
            }
        }
    }

    public class MyCommandInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result
        )
        {
            if (command.CommandText.Contains("UnionQuery"))
            {
                command.CommandText = command.CommandText + Environment.NewLine + "OPTION (MERGE UNION)";
            }

            return base.ReaderExecuting(command, eventData, result);
        }

        public override Task<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default
        )
        {
            if (command.CommandText.Contains("UnionQuery"))
            {
                command.CommandText = command.CommandText + Environment.NewLine + "OPTION (MERGE UNION)";
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    public class Author
    {
        public int AuthorId { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public ICollection<Blog> Blogs { get; set; } = new HashSet<Blog>();
        public override string ToString() => $"{Name} {LastName}";
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public Author Author { get; set; }
        public ICollection<Post> Posts { get; set; } = new HashSet<Post>();
    }

    public class Post
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public DateTime PublishDate { get; set; }
        public Blog Blog { get; set; }
    }
}
