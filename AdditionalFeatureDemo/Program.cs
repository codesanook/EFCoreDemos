using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MultiFeatureDemo22
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (var context = new PostContext())
            {
                // Drop database for clean state
                context.Database.EnsureDeleted();

                if (context.Database.EnsureCreated())
                {
                    var blog = new Blog
                    {
                        //Name = ".NET Blog",
                        Url = "https://blogs.msdn.microsoft.com/dotnet",
                        Author = "Codesanook Team",
                        Posts =
                        {
                            new Post
                            {
                                Title = "Announcing Entity Framework Core 3.0",
                                PublishDate = new DateTime(2019, 9, 12)
                            }
                        }
                    };

                    context.Blogs.Add(blog);
                    context.SaveChanges();

                    Console.WriteLine($"blogId = {blog.BlogId}");
                }
            }
        }

    }


    public class PostContext : DbContext
    {
        private readonly bool _logCommand;

        private static ILoggerFactory ContextLoggerFactory => new ConsoleLoggerFactory();

        public PostContext(bool logCommand = false) => _logCommand = logCommand;

        // Declare DBSets
        //null forgiving operator turns off the compiler-checks
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Blog> Blogs { get; set; } = null!;

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
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure model
        }

        private class ConsoleLoggerFactory : ILoggerFactory
        {
            private readonly SqlLogger _logger;
            public ConsoleLoggerFactory() => _logger = new SqlLogger();

            public void AddProvider(ILoggerProvider provider)
            {
            }

            public ILogger CreateLogger(string categoryName) => _logger;

            public void Dispose()
            {
            }

            private class SqlLogger : ILogger
            {
                public IDisposable BeginScope<TState>(TState state) => null;

                public bool IsEnabled(LogLevel logLevel) => true;

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


    public class Blog
    {
        public int BlogId { get; set; }
        public string? Name { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string Author { get; set; } = null!;
        public ICollection<Post> Posts { get; set; } = new HashSet<Post>();
    }

    public class Post
    {
        public int PostId { get; set; }
        public string Title { get; set; } = null!;
        public DateTime PublishDate { get; set; }
    }
}
