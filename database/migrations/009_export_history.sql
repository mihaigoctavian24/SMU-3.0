-- =============================================
-- Migration: Export History Feature
-- Description: Add export_histories table and export_type enum
-- Version: 009
-- Date: 2025-12-01
-- =============================================

-- Create export_type enum
CREATE TYPE export_type AS ENUM (
    'SituatieScolara',
    'AdeverintaStudent',
    'CatalogNote',
    'RaportFacultate',
    'StudentsExcel',
    'GradesExcel',
    'AttendanceExcel',
    'ActivityLogExcel'
);

-- Create export_histories table
CREATE TABLE export_histories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    export_type export_type NOT NULL,
    file_name VARCHAR(500) NOT NULL,
    parameters JSONB,
    file_size BIGINT NOT NULL,
    download_count INTEGER NOT NULL DEFAULT 0,
    file_path VARCHAR(1000),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Create indexes for performance
CREATE INDEX idx_export_histories_user_id ON export_histories(user_id);
CREATE INDEX idx_export_histories_export_type ON export_histories(export_type);
CREATE INDEX idx_export_histories_created_at ON export_histories(created_at);
CREATE INDEX idx_export_histories_is_deleted ON export_histories(is_deleted);

-- Add comments
COMMENT ON TABLE export_histories IS 'Tracks all document exports for audit and re-download';
COMMENT ON COLUMN export_histories.parameters IS 'JSON parameters used to generate the export (for re-generation)';
COMMENT ON COLUMN export_histories.file_path IS 'Optional path to stored file (null = regenerate on-demand)';
COMMENT ON COLUMN export_histories.expires_at IS 'Expiration date for automatic cleanup (default 30 days)';
COMMENT ON COLUMN export_histories.is_deleted IS 'Soft delete flag';

-- Grant permissions
GRANT SELECT, INSERT, UPDATE ON export_histories TO authenticated;
GRANT DELETE ON export_histories TO service_role;
