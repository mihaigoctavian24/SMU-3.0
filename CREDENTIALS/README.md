# Credentials Security

⚠️ **WARNING: This folder should NEVER be committed to version control!** ⚠️

## Purpose

This folder is intended for local development credentials only. It should contain sensitive information such as:

- API Keys
- Database Connection Strings
- Service Account Credentials
- JWT Secrets
- Other sensitive configuration data

## Security Guidelines

1. **Never commit this folder to git**
   - The `.gitignore` file should exclude this folder
   - Verify with `git status` that files in this folder are not tracked

2. **Local Only**
   - This folder and its contents should only exist on your local development machine
   - Do not share these files with anyone
   - Do not store them in cloud storage unless encrypted

3. **Environment Variables Preferred**
   - Use environment variables instead of files when possible
   - For local development, use `.env` files (also excluded from git)
   - In production, use platform-specific secret management (Azure Key Vault, AWS Secrets Manager, etc.)

4. **Credential Rotation**
   - Rotate credentials regularly
   - Immediately rotate any credentials that may have been exposed
   - Use temporary credentials when possible

## Setup Instructions

1. Create a `.env` file in the project root (copy from `.env.example`)
2. Fill in your actual credentials in the `.env` file
3. Load environment variables in your application code
4. Never commit the `.env` file

## Emergency Procedures

If credentials are accidentally committed:

1. Immediately rotate all exposed credentials
2. Revoke compromised keys/tokens
3. Check git history for exposure
4. Notify security team if applicable
5. Update this file with incident details