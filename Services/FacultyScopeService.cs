using EduCore.Data;
using EduCore.Models.Entities;
using EduCore.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduCore.Services;

public class FacultyScopeService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public FacultyScopeService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<Faculty?> GetMyFacultyAsync(ClaimsPrincipal user)
    {
        var userId = _users.GetUserId(user);
        var appUser = await _users.GetUserAsync(user);
        var userEmail = appUser?.Email;

        if (userId == null && string.IsNullOrEmpty(userEmail)) return null;

        return await _db.Faculty
            .Include(f => f.Person)
            .FirstOrDefaultAsync(f => f.Person != null &&
                ((f.Person.User != null && f.Person.User.Id == userId) ||
                 (!string.IsNullOrEmpty(userEmail) && f.Person.Email != null && f.Person.Email.ToLower() == userEmail.ToLower())));
    }

    public async Task<List<Section>> GetMySectionsAsync(int facultyId)
    {
        return await _db.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.SchoolYear)
            .Where(s => s.AdviserId == facultyId
                     || s.Subjects.Any(ss => ss.FacultyId == facultyId))
            .OrderBy(s => s.GradeLevel!.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Student>> GetMyStudentsAsync(int facultyId)
    {
        var sections = await GetMySectionsAsync(facultyId);
        var sectionIds = sections.Select(s => s.Id).ToList();

        return await _db.Students
            .Include(st => st.Person)
            .Include(st => st.Section)
                .ThenInclude(sec => sec!.GradeLevel)
            .Where(st => st.SectionId != null && sectionIds.Contains(st.SectionId.Value))
            .OrderBy(st => st.Person!.LastName)
            .ThenBy(st => st.Person!.FirstName)
            .ToListAsync();
    }

    public async Task<PagedResult<Student>> GetMyStudentsPagedAsync(int facultyId, FacultyStudentQuery query)
    {
        var sections = await GetMySectionsAsync(facultyId);
        var sectionIds = sections.Select(s => s.Id).ToList();

        var baseQuery = _db.Students
            .Include(st => st.Person)
            .Include(st => st.Section)
                .ThenInclude(sec => sec!.GradeLevel)
            .Where(st => st.SectionId != null && sectionIds.Contains(st.SectionId.Value));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(st =>
                (st.Person != null && st.Person.FirstName != null && st.Person.FirstName.ToLower().Contains(s)) ||
                (st.Person != null && st.Person.LastName != null && st.Person.LastName.ToLower().Contains(s)) ||
                (st.StudentNumber != null && st.StudentNumber.ToLower().Contains(s)) ||
                (st.Email != null && st.Email.ToLower().Contains(s)));
        }

        if (query.SectionId.HasValue)
        {
            baseQuery = baseQuery.Where(st => st.SectionId == query.SectionId.Value);
        }

        // Sorting
        var sortColumn = query.SortColumn?.ToLower();
        bool desc = query.SortDesc;
        baseQuery = (sortColumn, desc) switch
        {
            ("studentnumber", false) => baseQuery.OrderBy(st => st.StudentNumber),
            ("studentnumber", true) => baseQuery.OrderByDescending(st => st.StudentNumber),
            ("section", false) => baseQuery.OrderBy(st => st.Section!.Name),
            ("section", true) => baseQuery.OrderByDescending(st => st.Section!.Name),
            ("grade", false) => baseQuery.OrderBy(st => st.Section!.GradeLevel!.Name),
            ("grade", true) => baseQuery.OrderByDescending(st => st.Section!.GradeLevel!.Name),
            ("firstname", false) => baseQuery.OrderBy(st => st.Person!.FirstName),
            ("firstname", true) => baseQuery.OrderByDescending(st => st.Person!.FirstName),
            _ => baseQuery.OrderByDescending(st => st.Id)
        };

        var totalCount = await baseQuery.CountAsync();
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 25 : query.PageSize;
        var items = await baseQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Student>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> CanAccessStudentAsync(int facultyId, int studentId)
    {
        var students = await GetMyStudentsAsync(facultyId);
        return students.Any(s => s.Id == studentId);
    }
}