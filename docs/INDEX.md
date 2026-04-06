# Documentation Index

## EPECPS Technical Documentation

Complete technical documentation for the Employee Performance Evaluation and Career Progression System.

## Documentation Structure

### 1. Database Structure
**File**: `DATABASE_STRUCTURE.md`

**Contents:**
- Complete database schema
- All tables with columns and data types
- Indexes and constraints
- Entity relationships diagram
- Foreign key relationships
- Connection string configurations
- Migration commands

**When to use:**
- Understanding data model
- Database design reference
- Migration planning
- Query optimization
- Schema updates

### 2. System Architecture
**File**: `ARCHITECTURE.md`

**Contents:**
- Overall system architecture
- Layer structure (Domain, Application, Infrastructure, API)
- Frontend architecture (Angular)
- Design patterns used
- Authentication and authorization flow
- Communication protocols
- Deployment architecture
- Security considerations

**When to use:**
- Understanding system design
- Onboarding new developers
- Architecture decisions
- Technology stack reference
- Integration planning

### 3. Evaluation Workflow
**File**: `EVALUATION_WORKFLOW.md`

**Contents:**
- Complete evaluation lifecycle
- Workflow states and transitions
- Role-specific actions
- Approval chain process
- Peer review process
- Promotion workflow
- Scoring calculations
- Notification triggers
- Business rules and validation

**When to use:**
- Understanding business process
- Workflow customization
- Troubleshooting status issues
- Training users
- Process documentation

### 4. System Features
**File**: `FEATURES.md`

**Contents:**
- Complete feature list
- Module descriptions
- User capabilities by role
- Integration points
- Security features
- Configuration options

**When to use:**
- Feature discovery
- User training
- Requirements verification
- Feature planning
- User documentation

### 5. API Reference
**File**: `API_REFERENCE.md`

**Contents:**
- All API endpoints
- Request/response formats
- Authentication requirements
- Authorization rules
- Error handling
- Query parameters
- Example requests

**When to use:**
- API integration
- Frontend development
- Testing
- Third-party integration
- API client development

### 6. Docker Dev Run Guide
**File**: `RUN_PROJECT_DOCKER_DEV.md`

**Contents:**
- One-command development startup (`./scripts/docker.sh up`)
- Cached builds for backend/frontend
- Backend auto-migration + auto-seeding flow
- Optional Excel test-data import flow
- Full refresh option (`./scripts/docker.sh up-fresh`)
- Rebuild, logs, reset, and status commands

**When to use:**
- Fast day-to-day local development
- Avoid repeated dependency downloads
- Standardized team run commands

## Quick Reference

### Common Tasks

**Setting up the database:**
1. Configure connection string in `appsettings.json`
2. Run migrations: `dotnet ef database update -s Epecps.Api -p ../Epecps.Infrastructure`
3. Verify tables in SQL Server Management Studio

**Understanding workflow:**
1. Review `EVALUATION_WORKFLOW.md`
2. Check status transitions
3. Identify role requirements
4. Review notification triggers

**Adding new feature:**
1. Check `ARCHITECTURE.md` for layer responsibilities
2. Add entities to Domain layer
3. Create DTOs in Application layer
4. Implement services in Infrastructure layer
5. Add controllers in API layer
6. Update `DATABASE_STRUCTURE.md` with new tables

**API integration:**
1. Review `API_REFERENCE.md` for endpoint
2. Check authentication requirements
3. Review request/response format
4. Implement in service layer
5. Add error handling

**Database changes:**
1. Modify entity in Domain layer
2. Create migration: `dotnet ef migrations add MigrationName`
3. Review generated migration
4. Apply migration: `dotnet ef database update`
5. Update `DATABASE_STRUCTURE.md`

## Documentation Maintenance

### Update Frequency

**DATABASE_STRUCTURE.md**
- Update when: Database schema changes
- Update by: Running migration and documenting changes
- Review: After each release

**ARCHITECTURE.md**
- Update when: Major architectural changes
- Update by: Architecture team
- Review: Quarterly

**EVALUATION_WORKFLOW.md**
- Update when: Business process changes
- Update by: Product owner + development team
- Review: After workflow modifications

**FEATURES.md**
- Update when: New features added
- Update by: Feature owner
- Review: Each sprint/release

**API_REFERENCE.md**
- Update when: API changes
- Update by: Backend developers
- Review: Each release

### Documentation Standards

**Format:**
- Use Markdown (.md) format
- Clear headings and structure
- Code examples with syntax highlighting
- Tables for structured data
- Diagrams using ASCII art or PlantUML

**Content:**
- Clear and concise
- Technical accuracy
- Practical examples
- Cross-references between documents
- Version information

**Maintenance:**
- Keep in sync with code
- Document breaking changes
- Include migration guides
- Note deprecations

## Related Documentation

**README.md** (Root)
- Getting started guide
- Installation instructions
- Running the application
- Configuration guide
- Troubleshooting

**DASHBOARD_IMPLEMENTATION.md** (Root)
- Dashboard feature details
- Implementation specifics
- Component structure

**DASHBOARD_ENHANCEMENTS.md** (Root)
- Dashboard improvements
- Future enhancements
- Enhancement roadmap

## Support and Resources

**Code Documentation:**
- XML comments in C# code
- JSDoc comments in TypeScript
- Inline code comments for complex logic

**API Documentation:**
- Swagger UI: https://localhost:7275/swagger
- Interactive API testing
- Schema definitions

**Database Documentation:**
- Entity Framework migrations
- SQL Server schema
- Database diagrams

## Version Information

**Documentation Version**: 1.0

**Last Updated**: January 2025

**System Version**: 1.0

**Compatibility:**
- .NET 8
- Angular 20
- SQL Server 2019+
- Azure AD

## Contributing to Documentation

**Process:**
1. Identify documentation gap
2. Create/update documentation file
3. Follow documentation standards
4. Add cross-references
5. Update this index if new file created
6. Submit for review

**Guidelines:**
- Write for your audience (developers, users, admins)
- Use clear, professional language
- Include practical examples
- Keep technical accuracy
- Maintain consistency

## Feedback

For documentation issues or suggestions:
- Create issue in repository
- Tag with "documentation"
- Provide specific feedback
- Suggest improvements

## Document Conventions

**Code Blocks:**
```language
Code example here
```

**Tables:**
Use markdown tables for structured data

**Emphasis:**
- **Bold** for important terms
- *Italic* for emphasis
- `Code` for inline code/commands

**Sections:**
- Use ## for main sections
- Use ### for subsections
- Use #### for sub-subsections

**Links:**
- Relative links for internal docs
- Full URLs for external resources
- Descriptive link text
