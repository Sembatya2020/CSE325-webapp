using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Data;
using MvcMovie.Models;

namespace MvcMovie.Controllers
{
    public class MoviesController : Controller
    {
        private readonly MvcMovieContext _context;

        public MoviesController(MvcMovieContext context)
        {
            _context = context;
        }

        // GET: Movies
    public async Task<IActionResult> Index(string movieGenre, string searchString, int? releaseYear)
        {
            if (_context.Movie == null)
            {
                return Problem("Entity set 'MvcMovieContext.Movie'  is null.");
            }

            // Repair any rows that have NULL for important string columns (Genre or Rating).
            // Use a direct SQL UPDATE to avoid materializing rows (which can trigger the
            // SqliteValueReader.GetString() call on NULL values). This updates the DB in-place
            // without reading the problematic NULL values into memory.
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE \"Movie\" SET \"Genre\" = 'Unknown' WHERE \"Genre\" IS NULL; " +
                "UPDATE \"Movie\" SET \"Rating\" = 'Unrated' WHERE \"Rating\" IS NULL;"
            );

            // Use LINQ to get list of genres. Exclude NULL genres to avoid SQLite GetString() errors when a row has a NULL.
            IQueryable<string> genreQuery = from m in _context.Movie
                                            where m.Genre != null
                                            orderby m.Genre
                                            select m.Genre!;

            var movies = from m in _context.Movie
                         select m;

            if (!string.IsNullOrEmpty(searchString))
            {
                movies = movies.Where(s => s.Title!.ToUpper().Contains(searchString.ToUpper()));
            }

            if (!string.IsNullOrEmpty(movieGenre))
            {
                movies = movies.Where(x => x.Genre == movieGenre);
            }

            if (releaseYear.HasValue)
            {
                movies = movies.Where(m => m.ReleaseDate.Year >= releaseYear.Value);
            }

            // Fix any rows in the database that have NULL Genre to avoid SQLite GetString() errors
            // (this updates the DB in-place once so subsequent reads won't fail).
            var moviesWithNullGenre = await _context.Movie.Where(m => m.Genre == null).ToListAsync();
            if (moviesWithNullGenre.Count > 0)
            {
                foreach (var m in moviesWithNullGenre)
                {
                    m.Genre = "Unknown";
                }
                await _context.SaveChangesAsync();

                // Recreate the queries now that data has been fixed
                genreQuery = from m in _context.Movie
                             where m.Genre != null
                             orderby m.Genre
                             select m.Genre!;
                movies = from m in _context.Movie
                         select m;
                if (!string.IsNullOrEmpty(searchString))
                {
                    movies = movies.Where(s => s.Title!.ToUpper().Contains(searchString.ToUpper()));
                }
                if (!string.IsNullOrEmpty(movieGenre))
                {
                    movies = movies.Where(x => x.Genre == movieGenre);
                }
                if (releaseYear.HasValue)
                {
                    movies = movies.Where(m => m.ReleaseDate.Year >= releaseYear.Value);
                }
            }

            var movieGenreVM = new MvcMovie.Models.MovieGenreViewModel
            {
                Genres = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await genreQuery.Distinct().ToListAsync()),
                Movies = await movies.ToListAsync(),
                MovieGenre = movieGenre,
                SearchString = searchString,
                ReleaseYear = releaseYear
            };

            return View(movieGenreVM);
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movie.FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();

            return View(movie);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Title,ReleaseDate,Genre,Price,Rating")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var movie = await _context.Movie.FindAsync(id);
            if (movie == null) return NotFound();
            return View(movie);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ReleaseDate,Genre,Price,Rating")] Movie movie)
        {
            if (id != movie.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Movie.Any(e => e.Id == movie.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var movie = await _context.Movie.FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();
            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
