using Epecps.Application.DTOs.AdminImport;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Epecps.Infrastructure.Services;

public class UserProjectImportService : IUserProjectImportService
{
    private readonly EpecpsDbContext _context;
    private readonly ILogger<UserProjectImportService> _logger;

    public UserProjectImportService(EpecpsDbContext context, ILogger<UserProjectImportService> logger)
    {
        _context = context;
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public byte[] GenerateTemplate()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("UsersProjectsImport");

        var headers = new[]
        {
            "employeeEmail",
            "employeeFullName",
            "departmentName",
            "userStatus",
            "systemRoles",
            "reportingManagerEmail",
            "projectCode",
            "projectName",
            "projectStatus",
            "assignmentRole",
            "assignmentStartDate",
            "assignmentEndDate",
            "projectManagerEmail",
            "supervisorEmail"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        using (var headerRange = worksheet.Cells[1, 1, 1, headers.Length])
        {
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        worksheet.Cells.AutoFitColumns();
        return package.GetAsByteArray();
    }

    public async Task<UsersProjectsImportResultDto> ImportAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var result = new UsersProjectsImportResultDto();
        var rows = new List<ImportRow>();

        using (var package = new ExcelPackage())
        {
            await package.LoadAsync(fileStream, cancellationToken);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                throw new InvalidOperationException("Excel file does not contain any worksheet.");
            }

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount < 2)
            {
                return result;
            }

            var columnMap = BuildColumnMap(worksheet);
            for (var row = 2; row <= rowCount; row++)
            {
                var importRow = ParseRow(worksheet, row, columnMap);
                if (importRow.IsEmpty)
                {
                    result.SkippedRows++;
                    continue;
                }

                result.TotalRows++;

                if (string.IsNullOrWhiteSpace(importRow.EmployeeEmail))
                {
                    result.Errors.Add(new UsersProjectsImportRowErrorDto
                    {
                        RowNumber = row,
                        Message = "employeeEmail is required."
                    });
                    continue;
                }

                if (!IsValidEmail(importRow.EmployeeEmail))
                {
                    result.Errors.Add(new UsersProjectsImportRowErrorDto
                    {
                        RowNumber = row,
                        Message = "employeeEmail is not a valid email."
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(importRow.ReportingManagerEmail))
                {
                    result.Errors.Add(new UsersProjectsImportRowErrorDto
                    {
                        RowNumber = row,
                        Message = "reportingManagerEmail is required."
                    });
                    continue;
                }

                if (!IsValidEmail(importRow.ReportingManagerEmail))
                {
                    result.Errors.Add(new UsersProjectsImportRowErrorDto
                    {
                        RowNumber = row,
                        Message = "reportingManagerEmail is not a valid email."
                    });
                    continue;
                }

                rows.Add(importRow);
            }
        }

        if (rows.Count == 0)
        {
            return result;
        }

        // Aggregate desired roles across all rows per employee so re-import performs
        // exact role replacement against the full Excel definition.
        var aggregatedRolesByEmployeeEmail = rows
            .GroupBy(r => NormalizeEmail(r.EmployeeEmail))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var row in g)
                    {
                        foreach (var role in ParseRoleNames(row.SystemRoles))
                        {
                            merged.Add(role);
                        }
                    }

                    if (merged.Count == 0)
                    {
                        merged.Add("Employee");
                    }

                    return string.Join(",", merged);
                },
                StringComparer.OrdinalIgnoreCase);

        var emailSet = rows
            .SelectMany(r => new[] { r.EmployeeEmail, r.ReportingManagerEmail, r.ProjectManagerEmail, r.SupervisorEmail })
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(NormalizeEmail)
            .Distinct()
            .ToList();

        var existingUsers = await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => emailSet.Contains(u.Email))
            .ToDictionaryAsync(u => u.Email, cancellationToken);

        var rolesByName = await _context.Roles
            .ToDictionaryAsync(r => r.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var row in rows)
        {
            try
            {
                aggregatedRolesByEmployeeEmail.TryGetValue(NormalizeEmail(row.EmployeeEmail), out var aggregatedRoleValue);
                await UpsertPersonAsync(
                    row.EmployeeEmail,
                    row.EmployeeFullName,
                    row.DepartmentName,
                    row.UserStatus,
                    true,
                    aggregatedRoleValue,
                    existingUsers,
                    rolesByName,
                    result,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(row.ProjectManagerEmail))
                {
                    await UpsertPersonAsync(row.ProjectManagerEmail, "Project Manager", row.DepartmentName, "Active", false, null, existingUsers, rolesByName, result, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(row.SupervisorEmail))
                {
                    await UpsertPersonAsync(row.SupervisorEmail, "Supervisor", row.DepartmentName, "Active", false, null, existingUsers, rolesByName, result, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process user upsert for row {RowNumber}", row.RowNumber);
                result.Errors.Add(new UsersProjectsImportRowErrorDto
                {
                    RowNumber = row.RowNumber,
                    Message = $"User upsert failed: {ex.Message}"
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var projectCodes = rows.Select(r => r.ProjectCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var projectsByCode = await _context.Projects
            .Where(p => projectCodes.Contains(p.ProjectCode))
            .ToDictionaryAsync(p => p.ProjectCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var assignments = await _context.UserProjectAssignments
            .Include(a => a.Project)
            .Where(a => projectCodes.Contains(a.Project.ProjectCode))
            .ToListAsync(cancellationToken);

        var assignmentByKey = assignments.ToDictionary(
            a => $"{a.UserId}:{a.ProjectId}",
            a => a,
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            try
            {
                var employeeEmail = NormalizeEmail(row.EmployeeEmail);
                if (!existingUsers.TryGetValue(employeeEmail, out var employee))
                {
                    result.Errors.Add(new UsersProjectsImportRowErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Message = $"Employee '{employeeEmail}' could not be resolved."
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.ProjectCode))
                {
                    // User/role/manager mapping rows are allowed without project assignment.
                    continue;
                }

                if (!projectsByCode.TryGetValue(row.ProjectCode, out var project))
                {
                    project = new Project
                    {
                        ProjectCode = row.ProjectCode,
                        ProjectName = string.IsNullOrWhiteSpace(row.ProjectName) ? row.ProjectCode : row.ProjectName,
                        Status = string.IsNullOrWhiteSpace(row.ProjectStatus) ? "Active" : row.ProjectStatus,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Projects.Add(project);
                    projectsByCode[row.ProjectCode] = project;
                    result.CreatedProjects++;
                }
                else
                {
                    var changed = false;
                    if (!string.IsNullOrWhiteSpace(row.ProjectName) && !string.Equals(project.ProjectName, row.ProjectName, StringComparison.Ordinal))
                    {
                        project.ProjectName = row.ProjectName;
                        changed = true;
                    }

                    if (!string.IsNullOrWhiteSpace(row.ProjectStatus) && !string.Equals(project.Status, row.ProjectStatus, StringComparison.Ordinal))
                    {
                        project.Status = row.ProjectStatus;
                        changed = true;
                    }

                    if (changed)
                    {
                        project.UpdatedAt = DateTime.UtcNow;
                        result.UpdatedProjects++;
                    }
                }

                if (!string.IsNullOrWhiteSpace(row.ProjectManagerEmail))
                {
                    var pmEmail = NormalizeEmail(row.ProjectManagerEmail);
                    if (existingUsers.TryGetValue(pmEmail, out var pmUser))
                    {
                        project.ProjectManagerUserId = pmUser.UserId;
                    }
                }

                if (!string.IsNullOrWhiteSpace(row.SupervisorEmail))
                {
                    var supervisorEmail = NormalizeEmail(row.SupervisorEmail);
                    if (existingUsers.TryGetValue(supervisorEmail, out var supervisorUser))
                    {
                        project.SupervisorUserId = supervisorUser.UserId;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                var key = $"{employee.UserId}:{project.ProjectId}";
                if (!assignmentByKey.TryGetValue(key, out var assignment))
                {
                    assignment = new UserProjectAssignment
                    {
                        UserId = employee.UserId,
                        ProjectId = project.ProjectId,
                        AssignmentRole = string.IsNullOrWhiteSpace(row.AssignmentRole) ? "Member" : row.AssignmentRole,
                        StartDate = row.AssignmentStartDate,
                        EndDate = row.AssignmentEndDate,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.UserProjectAssignments.Add(assignment);
                    assignmentByKey[key] = assignment;
                    result.CreatedAssignments++;
                }
                else
                {
                    assignment.AssignmentRole = string.IsNullOrWhiteSpace(row.AssignmentRole) ? assignment.AssignmentRole : row.AssignmentRole;
                    assignment.StartDate = row.AssignmentStartDate ?? assignment.StartDate;
                    assignment.EndDate = row.AssignmentEndDate;
                    assignment.IsActive = true;
                    assignment.UpdatedAt = DateTime.UtcNow;
                    result.UpdatedAssignments++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process project upsert for row {RowNumber}", row.RowNumber);
                result.Errors.Add(new UsersProjectsImportRowErrorDto
                {
                    RowNumber = row.RowNumber,
                    Message = $"Project/assignment upsert failed: {ex.Message}"
                });
            }
        }

        // Upsert employee -> reporting manager mappings from reportingManagerEmail
        var mappingKeys = rows
            .Select(r => new { EmployeeEmail = NormalizeEmail(r.EmployeeEmail), ManagerEmail = NormalizeEmail(r.ReportingManagerEmail) })
            .Distinct()
            .ToList();

        var employeeIds = mappingKeys
            .Where(x => existingUsers.ContainsKey(x.EmployeeEmail))
            .Select(x => existingUsers[x.EmployeeEmail].UserId)
            .Distinct()
            .ToList();

        var managerIds = mappingKeys
            .Where(x => existingUsers.ContainsKey(x.ManagerEmail))
            .Select(x => existingUsers[x.ManagerEmail].UserId)
            .Distinct()
            .ToList();

        var existingMappings = await _context.UserManagerMappings
            .Where(m => employeeIds.Contains(m.EmployeeUserId) && managerIds.Contains(m.ManagerUserId))
            .ToDictionaryAsync(m => $"{m.EmployeeUserId}:{m.ManagerUserId}", cancellationToken);

        foreach (var row in rows)
        {
            try
            {
                var employeeEmail = NormalizeEmail(row.EmployeeEmail);
                var managerEmail = NormalizeEmail(row.ReportingManagerEmail);

                if (!existingUsers.TryGetValue(employeeEmail, out var employee))
                {
                    result.Errors.Add(new UsersProjectsImportRowErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Message = $"Employee '{employeeEmail}' could not be resolved for manager mapping."
                    });
                    continue;
                }

                if (!existingUsers.TryGetValue(managerEmail, out var manager))
                {
                    result.Errors.Add(new UsersProjectsImportRowErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Message = $"Unknown manager '{managerEmail}'. Add this user in Excel before assigning as reporting manager."
                    });
                    continue;
                }

                var mappingKey = $"{employee.UserId}:{manager.UserId}";
                if (existingMappings.TryGetValue(mappingKey, out var existingMapping))
                {
                    existingMapping.UpdatedAt = DateTime.UtcNow;
                    result.UpdatedManagerMappings++;
                    continue;
                }

                var mapping = new UserManagerMapping
                {
                    EmployeeUserId = employee.UserId,
                    ManagerUserId = manager.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserManagerMappings.Add(mapping);
                existingMappings[mappingKey] = mapping;
                result.CreatedManagerMappings++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process manager mapping for row {RowNumber}", row.RowNumber);
                result.Errors.Add(new UsersProjectsImportRowErrorDto
                {
                    RowNumber = row.RowNumber,
                    Message = $"Manager mapping failed: {ex.Message}"
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task UpsertPersonAsync(
        string email,
        string fullName,
        string departmentName,
        string userStatus,
        bool updateUserProfile,
        string? systemRoles,
        IDictionary<string, User> existingUsers,
        IDictionary<string, Role> rolesByName,
        UsersProjectsImportResultDto result,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return;
        }

        if (!existingUsers.TryGetValue(normalizedEmail, out var user))
        {
            var departmentId = await ResolveDepartmentIdAsync(departmentName, cancellationToken);
            user = new User
            {
                Email = normalizedEmail,
                FullName = string.IsNullOrWhiteSpace(fullName) ? normalizedEmail.Split('@')[0] : fullName.Trim(),
                Status = string.IsNullOrWhiteSpace(userStatus) ? "Active" : userStatus.Trim(),
                DeptId = departmentId,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            existingUsers[normalizedEmail] = user;
            result.CreatedUsers++;
        }
        else if (updateUserProfile)
        {
            var changed = false;
            if (!string.IsNullOrWhiteSpace(fullName) && !string.Equals(user.FullName, fullName.Trim(), StringComparison.Ordinal))
            {
                user.FullName = fullName.Trim();
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(userStatus) && !string.Equals(user.Status, userStatus.Trim(), StringComparison.Ordinal))
            {
                user.Status = userStatus.Trim();
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(departmentName))
            {
                var departmentId = await ResolveDepartmentIdAsync(departmentName, cancellationToken);
                if (user.DeptId != departmentId)
                {
                    user.DeptId = departmentId;
                    changed = true;
                }
            }

            if (changed)
            {
                result.UpdatedUsers++;
            }
        }

        if (updateUserProfile)
        {
            var roleNames = ParseRoleNames(systemRoles);
            if (roleNames.Count == 0)
            {
                roleNames.Add("Employee");
            }

            var desiredRoleIds = new HashSet<int>();
            foreach (var roleName in roleNames)
            {
                if (!rolesByName.TryGetValue(roleName, out var role))
                {
                    role = new Role { Name = roleName };
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync(cancellationToken);
                    rolesByName[roleName] = role;
                }

                desiredRoleIds.Add(role.RoleId);
            }

            var currentUserRoleIds = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.RoleId)
                .ToListAsync(cancellationToken);

            foreach (var desiredRoleId in desiredRoleIds)
            {
                var trackedUserRoleExists = _context.UserRoles.Local.Any(
                    ur => ur.UserId == user.UserId && ur.RoleId == desiredRoleId);

                if (!currentUserRoleIds.Contains(desiredRoleId) && !trackedUserRoleExists)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = desiredRoleId
                    });
                    result.CreatedRoleAssignments++;
                }
            }

            var roleIdsToRemove = currentUserRoleIds
                .Where(currentRoleId => !desiredRoleIds.Contains(currentRoleId))
                .ToList();

            if (roleIdsToRemove.Count > 0)
            {
                var userRolesToRemove = await _context.UserRoles
                    .Where(ur => ur.UserId == user.UserId && roleIdsToRemove.Contains(ur.RoleId))
                    .ToListAsync(cancellationToken);

                if (userRolesToRemove.Count > 0)
                {
                    _context.UserRoles.RemoveRange(userRolesToRemove);
                    result.RemovedRoleAssignments += userRolesToRemove.Count;
                }
            }

            // If this imported user has HOD role, map them to the resolved department.
            if (roleNames.Contains("HOD") && user.DeptId > 0)
            {
                var existingHodMapping = await _context.DepartmentHodMappings.AnyAsync(
                    m => m.DeptId == user.DeptId && m.HodUserId == user.UserId,
                    cancellationToken);

                if (!existingHodMapping)
                {
                    _context.DepartmentHodMappings.Add(new DepartmentHodMapping
                    {
                        DeptId = user.DeptId,
                        HodUserId = user.UserId,
                        CreatedAt = DateTime.UtcNow
                    });
                    result.CreatedDepartmentHodMappings++;
                }
            }
        }
    }

    private async Task<int> ResolveDepartmentIdAsync(string departmentName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(departmentName))
        {
            var normalizedName = departmentName.Trim();
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == normalizedName, cancellationToken);
            if (department == null)
            {
                department = new Department { Name = normalizedName };
                _context.Departments.Add(department);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return department.DeptId;
        }

        var existingDepartment = await _context.Departments.OrderBy(d => d.DeptId).FirstOrDefaultAsync(cancellationToken);
        if (existingDepartment != null)
        {
            return existingDepartment.DeptId;
        }

        var fallback = new Department { Name = "General" };
        _context.Departments.Add(fallback);
        await _context.SaveChangesAsync(cancellationToken);
        return fallback.DeptId;
    }

    private static Dictionary<string, int> BuildColumnMap(ExcelWorksheet worksheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var columnCount = worksheet.Dimension?.Columns ?? 0;
        for (var col = 1; col <= columnCount; col++)
        {
            var value = worksheet.Cells[1, col].Text?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                map[value] = col;
            }
        }

        return map;
    }

    private static ImportRow ParseRow(ExcelWorksheet worksheet, int rowNumber, Dictionary<string, int> columnMap)
    {
        var row = new ImportRow
        {
            RowNumber = rowNumber,
            EmployeeEmail = ReadString(worksheet, rowNumber, columnMap, "employeeEmail"),
            EmployeeFullName = ReadString(worksheet, rowNumber, columnMap, "employeeFullName"),
            DepartmentName = ReadString(worksheet, rowNumber, columnMap, "departmentName"),
            UserStatus = ReadString(worksheet, rowNumber, columnMap, "userStatus"),
            SystemRoles = ReadString(worksheet, rowNumber, columnMap, "systemRoles"),
            ReportingManagerEmail = ReadString(worksheet, rowNumber, columnMap, "reportingManagerEmail"),
            ProjectCode = ReadString(worksheet, rowNumber, columnMap, "projectCode"),
            ProjectName = ReadString(worksheet, rowNumber, columnMap, "projectName"),
            ProjectStatus = ReadString(worksheet, rowNumber, columnMap, "projectStatus"),
            AssignmentRole = ReadString(worksheet, rowNumber, columnMap, "assignmentRole"),
            ProjectManagerEmail = ReadString(worksheet, rowNumber, columnMap, "projectManagerEmail"),
            SupervisorEmail = ReadString(worksheet, rowNumber, columnMap, "supervisorEmail"),
            AssignmentStartDate = ReadDate(worksheet, rowNumber, columnMap, "assignmentStartDate"),
            AssignmentEndDate = ReadDate(worksheet, rowNumber, columnMap, "assignmentEndDate")
        };

        if (!string.IsNullOrWhiteSpace(row.EmployeeEmail))
        {
            row.EmployeeEmail = NormalizeEmail(row.EmployeeEmail);
        }

        if (!string.IsNullOrWhiteSpace(row.ProjectManagerEmail))
        {
            row.ProjectManagerEmail = NormalizeEmail(row.ProjectManagerEmail);
        }

        if (!string.IsNullOrWhiteSpace(row.ReportingManagerEmail))
        {
            row.ReportingManagerEmail = NormalizeEmail(row.ReportingManagerEmail);
        }

        if (!string.IsNullOrWhiteSpace(row.SupervisorEmail))
        {
            row.SupervisorEmail = NormalizeEmail(row.SupervisorEmail);
        }

        if (!string.IsNullOrWhiteSpace(row.ProjectCode))
        {
            row.ProjectCode = row.ProjectCode.Trim();
        }

        return row;
    }

    private static string ReadString(ExcelWorksheet worksheet, int rowNumber, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var col))
        {
            return string.Empty;
        }

        return worksheet.Cells[rowNumber, col].Text?.Trim() ?? string.Empty;
    }

    private static DateTime? ReadDate(ExcelWorksheet worksheet, int rowNumber, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var col))
        {
            return null;
        }

        var value = worksheet.Cells[rowNumber, col].Value;
        if (value is DateTime dateTime)
        {
            return dateTime.Date;
        }

        if (value is double numeric)
        {
            return DateTime.FromOADate(numeric).Date;
        }

        var text = worksheet.Cells[rowNumber, col].Text?.Trim();
        if (DateTime.TryParse(text, out var parsed))
        {
            return parsed.Date;
        }

        return null;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static HashSet<string> ParseRoleNames(string? roles)
    {
        if (string.IsNullOrWhiteSpace(roles))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return roles
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ImportRow
    {
        public int RowNumber { get; set; }
        public string EmployeeEmail { get; set; } = string.Empty;
        public string EmployeeFullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string UserStatus { get; set; } = string.Empty;
        public string? SystemRoles { get; set; }
        public string ReportingManagerEmail { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectStatus { get; set; } = string.Empty;
        public string AssignmentRole { get; set; } = string.Empty;
        public DateTime? AssignmentStartDate { get; set; }
        public DateTime? AssignmentEndDate { get; set; }
        public string ProjectManagerEmail { get; set; } = string.Empty;
        public string SupervisorEmail { get; set; } = string.Empty;

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(EmployeeEmail) &&
            string.IsNullOrWhiteSpace(ReportingManagerEmail) &&
            string.IsNullOrWhiteSpace(ProjectCode) &&
            string.IsNullOrWhiteSpace(ProjectName);
    }
}
