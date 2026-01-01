# My Prompts

A collection of saved prompts for use with Claude Code and other AI tools.

---

## Template

```
### [Prompt Name]

[Your prompt text here]

---
```

## Prompts

<!-- Add your prompts below -->

### Add New Feature

Create a [type] feature for the [project area].
Requirements:

- Uses Lowroller database
- Follows existing code patterns in src/[directory]
- Includes TypeScript types
- Has error handling

### Example Feature Add: Create Contact Form Component

Create a contact form component that sends emails via Container App Notifications worker.

Requirements:

- Name, email, message fields
- Client-side validation
- Rate limiting (1 submission per minute per IP)
- Success/error feedback

Technical details:

- Use PrimeNg Components
- Follow accessibility best practices
- Include proper error handling

Please provide:

1. Component code
2. TypeScript types
3. Example usage
4. Brief explanation of approach

### Example Feature Add: User Authentication Feature

Implement user authentication for [application type].

Features needed:

- User registration (email + password)
- Login/logout
- Password hashing (bcrypt)
- Session management
- Protected routes

Tech stack:

- [Framework: ASP.NET Core Web API/Next/etc]
- [Database: Azure SQL/PostgreSQL/etc]
- [Session storage: KV/cookies/etc]

Security requirements:

- Secure password hashing
- CSRF protection
- Rate limiting on login attempts
- Secure session management

Please provide:

1. Authentication logic
2. Database schema
3. Protected route example
4. Security best practices followed

### Debug Performance

This [component/function] is slow:
[code]
Expected: [metric]
Actual: [metric]
Please optimize.

### Review Code

Please review this code:

[Paste code]

Focus on:

- Code quality
- Best practices
- Potential bugs
- Performance
- Security
- Accessibility (if UI)

Please provide:

1. Specific issues found
2. Severity (critical/major/minor)
3. Suggested fixes
4. General feedback

### Explain Code

Explain how this code works:

[Paste code]

Please explain:

- What it does (high level)
- How it works (step by step)
- Why it's written this way
- Any gotchas or edge cases
- How to modify it if needed

Audience level: [Beginner/Intermediate/Advanced]

### Best Practices

What are the best practices for [topic]?

Context:

- Tech stack: [technologies]
- Use case: [description]
- Team size: [if relevant]

Please cover:

- Industry standards
- Common pitfalls to avoid
- Tools and resources
- Code examples
- When to deviate from best practices

### Architecture Decision

Help me decide between [Option A] and [Option B] for [use case].

Requirements:

- [Requirement 1]
- [Requirement 2]
- [Requirement 3]

Constraints:

- [Constraint 1: e.g., "Budget: $0-20/mo"]
- [Constraint 2: e.g., "Team size: 2"]
- [Constraint 3: e.g., "Timeline: 2 weeks"]

Please provide:

1. Comparison of both options
2. Pros and cons of each
3. Recommendation with reasoning
4. Implementation considerations

### Migration Strategy

Help me migrate from [Old Technology] to [New Technology].

Current setup:

- [Current architecture]
- [Current tech stack]
- [Current data]

Target:

- [New architecture]
- [New tech stack]
- [Requirements]

Constraints:

- Zero downtime required: [Yes/No]
- Data migration needed: [Yes/No]
- Timeline: [duration]

Please provide:

1. Step-by-step migration plan
2. Risks and mitigations
3. Testing strategy
4. Rollback plan
5. Timeline estimate

### Accessibility Review

Review this component for accessibility:

[Paste component code]

Please check:

- WCAG 2.1 Level AA compliance
- Keyboard navigation
- Screen reader support
- Color contrast
- ARIA attributes
- Focus management
- Form accessibility

Provide:

1. Issues found
2. WCAG criteria violated
3. Specific fixes
4. Testing recommendations

### Security Review

Review this code for security vulnerabilities:

[Paste code]

Context:

- Purpose: [what it does]
- User input: [what users can input]
- Data handling: [how data is processed]

Please check for:

- SQL injection
- XSS vulnerabilities
- CSRF issues
- Authentication/authorization issues
- Data exposure
- Input validation
- Sensitive data handling

Provide:

1. Vulnerabilities found
2. Severity level
3. Exploit scenarios
4. Fixes

### README Template

Create a README.md for this project:

Project name: [name]
Description: [brief description]
Tech stack: [technologies used]

Please include:

- Project overview
- Features
- Prerequisites
- Installation instructions
- Usage examples
- Configuration
- Deployment
- Contributing guidelines
- License

### API Documentation

Document this API endpoint:

[Paste endpoint code]

Please create:

- Endpoint description
- HTTP method
- URL path
- Request parameters
- Request body (if applicable)
- Response format
- Status codes
- Error responses
- Example requests
- Example responses

### Write Documentation

Write documentation for this code:

[Paste code]

Audience: [Developers/end-users/team members]

Please include:

- Overview of what it does
- How to use it
- Parameters/props explanation
- Return values
- Examples
- Common pitfalls
- Related functions/components

### Test Debugging

This test is failing:

[Paste test code]

Error:
[Paste error message]

Code being tested:
[Paste relevant code]

Please:

1. Identify why test is failing
2. Fix the test (or the code if that's the issue)
3. Explain the fix

### Write Integration Tests

Write integration tests for this feature:

[Description of feature]

Components involved:

- [Component 1]
- [Component 2]
- [Component 3]

Test framework: [Playwright/Cypress/etc]

Please test:

- User workflows
- Error scenarios
- Edge cases
- Accessibility

Please provide:

1. Test file
2. Test setup instructions
3. Any fixtures/mocks needed

### Write Unit Tests

Write unit tests for this code:

[Paste code to test]

Test framework: [Jest/Vitest/etc]

Please test:

- Happy path (expected inputs)
- Edge cases
- Error conditions
- Boundary conditions

Please provide:

1. Complete test file
2. Test descriptions
3. Any test utilities/helpers needed
4. Coverage report interpretation

### Modernize Legacy Code

Modernize this code using current best practices:

[Paste legacy code]

Current issues:

- [Issue 1: e.g., "Uses var instead of const/let"]
- [Issue 2: e.g., "Callbacks instead of async/await"]
- [Issue 3: e.g., "Old API usage"]

Target:

- Use modern JavaScript/TypeScript
- Follow current best practices
- Maintain backward compatibility [if needed]

Please provide:

1. Modernized code
2. Explanation of changes
3. Migration notes

### Performance Optimization

Optimize this code for performance:

[Paste code]

Current issues:

- [Issue 1: e.g., "Renders too often"]
- [Issue 2: e.g., "Large bundle size"]
- [Issue 3: e.g., "Slow on mobile"]

Please provide:

1. Optimized code
2. Explanation of optimizations
3. Performance comparison
4. Trade-offs (if any)

### Extract Reusable Component

This code appears in multiple places:

[Paste repeated code]

Please:

1. Extract a reusable component/function
2. Show how to use it in both locations
3. Include proper TypeScript types
4. Suggest a good name

### Code Cleanup

Refactor this code to improve [quality aspect]:

[Paste code]

Goals:

- Improve readability
- Reduce complexity
- Follow best practices
- Maintain same functionality

Please provide:

1. Refactored code
2. Explanation of changes
3. Why the new version is better

### Debug Console Error

I see this error in the console:

[Paste console error]

Browser console shows:
[Paste stack trace if available]

What I'm doing when it happens:
[Description]

Relevant code:
[Paste code]

Please help me:

1. Understand what's causing this
2. Fix the error
3. Prevent similar errors

### Debug Unexpected Behavior

My [feature] isn't working as expected.

Expected behavior:
[Describe what should happen]

Actual behavior:
[Describe what's happening]

Code:
[Paste relevant code]

Steps to reproduce:

1. [Step 1]
2. [Step 2]
3. [Step 3]

Please:

1. Identify the issue
2. Explain why it's happening
3. Provide a fix
4. Suggest tests to prevent regression

### Debug Performance Issue

My [page/component/function] is running slowly.

Performance metrics:

- Current: [metric and value]
- Target: [desired metric]

Code:
[Paste relevant code]

What I've tried:

- [Attempt 1]
- [Attempt 2]

Please:

1. Identify performance bottlenecks
2. Suggest optimizations
3. Show optimized code
4. Explain performance improvements

### Debug Specific Error

I'm getting this error:

[Paste exact error message]

Context:

- Code: [paste relevant code]
- What I was doing: [description]
- What I expected: [expected behavior]
- What happened: [actual behavior]

Environment:

- Framework: [ASP.NET Core Web API/Angular/etc]
- Browser: [if relevant]
- Node version: [if relevant]

Please:

1. Explain what's causing the error
2. Provide a fix
3. Explain why the fix works

### API Integration

Integrate [API name] API into [application].

What I need:

- Fetch [data type] from API
- Handle authentication ([auth type])
- Cache responses ([caching strategy])
- Handle errors gracefully

API details:

- Endpoint: [URL]
- Auth method: [API key/OAuth/etc]
- Rate limits: [limit details]

Please provide:

1. API client code
2. Type definitions for responses
3. Error handling
4. Caching implementation
5. Usage examples

### CRUD Operations

Create CRUD operations for [entity name].

Entity fields:

- [field1]: [type]
- [field2]: [type]
- [field3]: [type]

Operations needed:

- Create [entity]
- Read/list [entities]
- Update [entity]
- Delete [entity]

Tech stack:

- Database: [Azure SQL/PostgreSQL/etc]
- API: [REST/GraphQL]
- Framework: [ASP.NET Core Web API/Angular/etc]

Please include:

1. Database schema/migration
2. API endpoints
3. Validation logic
4. Error handling
5. Example usage
