# Inspect Command

## Purpose

Perform initial inspection of the repository and .claude directory to understand existing structure and configuration before starting work.

## Usage

Use this command at the very beginning of any work session to familiarize yourself with the current state of the repository and Claude Code configuration.

## What It Does

1. Lists the .claude directory structure showing all available agents, commands, memory, playbooks, rules, skills, settings, and scripts
2. Checks for the existence of key files and directories
3. Shows the current git branch and recent commit history
4. Displays the solution structure and key project files
5. Reviews the database.sql file for Contract-related tables (if working on Contract module)
6. Examines the Workflow module as reference implementation (if working on Contract module)
7. Shows current Claude Code settings and permissions
8. Provides recommendations for next steps based on findings

## Output

Provides:

- .claude directory structure and file inventory
- Current git status and branch information
- Solution overview and project structure
- Database schema inspection (Contract tables if applicable)
- Workflow module reference overview (if applicable)
- Current Claude Code configuration summary
- Recommended next steps and available commands

## Example

Before starting any work:

1. Run this inspect command
2. Review the output showing:
   - What .claude files are available
   - Current git branch and recent commits
   - Solution structure
   - Database schema (for Contract work)
   - Workflow module structure (for Contract work)
   - Current settings and permissions
3. Based on findings, decide on next appropriate command:
   - For Contract module work: run `/contract` command
   - For general work: consider `/implement-feature` or `/review` as appropriate
   - For bug fixing: consider `/bug-fix` command
   - For preparing PR: consider `/pull-request` command

This ensures you begin work with complete context of both the repository and available Claude Code capabilities.
