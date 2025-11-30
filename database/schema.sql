-- ============================================
-- SMU 3.0 - Database Schema
-- Supabase PostgreSQL
-- Version: 2.0
-- Created: 30 Noiembrie 2024
-- ============================================

-- ============================================
-- CLEANUP: Drop existing tables (if reset needed)
-- ============================================
-- Run this section ONLY if you want to reset the database completely

/*
DROP TABLE IF EXISTS student_engagement CASCADE;
DROP TABLE IF EXISTS student_risk_scores CASCADE;
DROP TABLE IF EXISTS historical_metrics CASCADE;
DROP TABLE IF EXISTS daily_snapshots CASCADE;
DROP TABLE IF EXISTS grade_snapshots CASCADE;
DROP TABLE IF EXISTS attendance_stats CASCADE;
DROP TABLE IF EXISTS audit_logs CASCADE;
DROP TABLE IF EXISTS notifications CASCADE;
DROP TABLE IF EXISTS document_requests CASCADE;
DROP TABLE IF EXISTS attendances CASCADE;
DROP TABLE IF EXISTS grades CASCADE;
DROP TABLE IF EXISTS schedule_entries CASCADE;
DROP TABLE IF EXISTS courses CASCADE;
DROP TABLE IF EXISTS students CASCADE;
DROP TABLE IF EXISTS professors CASCADE;
DROP TABLE IF EXISTS groups CASCADE;
DROP TABLE IF EXISTS programs CASCADE;
DROP TABLE IF EXISTS faculties CASCADE;
DROP TABLE IF EXISTS asp_net_user_tokens CASCADE;
DROP TABLE IF EXISTS asp_net_user_roles CASCADE;
DROP TABLE IF EXISTS asp_net_user_logins CASCADE;
DROP TABLE IF EXISTS asp_net_user_claims CASCADE;
DROP TABLE IF EXISTS asp_net_role_claims CASCADE;
DROP TABLE IF EXISTS asp_net_roles CASCADE;
DROP TABLE IF EXISTS asp_net_users CASCADE;
DROP TABLE IF EXISTS academic_years CASCADE;

DROP TYPE IF EXISTS user_role CASCADE;
DROP TYPE IF EXISTS student_status CASCADE;
DROP TYPE IF EXISTS program_type CASCADE;
DROP TYPE IF EXISTS grade_type CASCADE;
DROP TYPE IF EXISTS grade_status CASCADE;
DROP TYPE IF EXISTS attendance_status CASCADE;
DROP TYPE IF EXISTS notification_type CASCADE;
DROP TYPE IF EXISTS request_status CASCADE;
DROP TYPE IF EXISTS request_type CASCADE;
DROP TYPE IF EXISTS risk_level CASCADE;
*/

-- ============================================
-- ENUMS
-- ============================================

CREATE TYPE user_role AS ENUM ('Student', 'Professor', 'Secretary', 'Dean', 'Rector', 'Admin');
CREATE TYPE student_status AS ENUM ('Active', 'Inactive', 'Graduated', 'Expelled', 'Suspended');
CREATE TYPE program_type AS ENUM ('Bachelor', 'Master', 'PhD');
CREATE TYPE grade_type AS ENUM ('Exam', 'Lab', 'Seminar', 'Project', 'Final');
CREATE TYPE grade_status AS ENUM ('Pending', 'Approved', 'Rejected');
CREATE TYPE attendance_status AS ENUM ('Present', 'Absent', 'Excused');
CREATE TYPE notification_type AS ENUM ('Info', 'Success', 'Warning', 'Error', 'GradeAdded', 'AttendanceMarked', 'RequestUpdate');
CREATE TYPE request_status AS ENUM ('Pending', 'InProgress', 'Approved', 'Rejected', 'Completed');
CREATE TYPE request_type AS ENUM ('StudentCertificate', 'GradeReport', 'EnrollmentProof', 'Other');
CREATE TYPE risk_level AS ENUM ('Low', 'Medium', 'High', 'Critical');

-- ============================================
-- ASP.NET IDENTITY TABLES
-- ============================================

CREATE TABLE asp_net_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(256),
    normalized_name VARCHAR(256) UNIQUE,
    concurrency_stamp TEXT
);

CREATE TABLE asp_net_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_name VARCHAR(256),
    normalized_user_name VARCHAR(256) UNIQUE,
    email VARCHAR(256),
    normalized_email VARCHAR(256),
    email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    password_hash TEXT,
    security_stamp TEXT,
    concurrency_stamp TEXT,
    phone_number TEXT,
    phone_number_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    two_factor_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    lockout_end TIMESTAMPTZ,
    lockout_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    access_failed_count INTEGER NOT NULL DEFAULT 0,
    -- Extended fields
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    role user_role NOT NULL DEFAULT 'Student',
    faculty_id UUID,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMPTZ
);

CREATE INDEX idx_users_email ON asp_net_users(normalized_email);
CREATE INDEX idx_users_role ON asp_net_users(role);
CREATE INDEX idx_users_faculty ON asp_net_users(faculty_id);

CREATE TABLE asp_net_role_claims (
    id SERIAL PRIMARY KEY,
    role_id UUID NOT NULL REFERENCES asp_net_roles(id) ON DELETE CASCADE,
    claim_type TEXT,
    claim_value TEXT
);

CREATE TABLE asp_net_user_claims (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    claim_type TEXT,
    claim_value TEXT
);

CREATE TABLE asp_net_user_logins (
    login_provider VARCHAR(128) NOT NULL,
    provider_key VARCHAR(128) NOT NULL,
    provider_display_name TEXT,
    user_id UUID NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    PRIMARY KEY (login_provider, provider_key)
);

CREATE TABLE asp_net_user_roles (
    user_id UUID NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES asp_net_roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE asp_net_user_tokens (
    user_id UUID NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    login_provider VARCHAR(128) NOT NULL,
    name VARCHAR(128) NOT NULL,
    value TEXT,
    PRIMARY KEY (user_id, login_provider, name)
);

-- ============================================
-- CORE TABLES
-- ============================================

-- Academic Years (pentru context temporal)
CREATE TABLE academic_years (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(20) NOT NULL, -- ex: "2024-2025"
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    is_current BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_academic_years_current ON academic_years(is_current) WHERE is_current = TRUE;

-- Faculties
CREATE TABLE faculties (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(20) NOT NULL UNIQUE,
    description TEXT,
    dean_id UUID REFERENCES asp_net_users(id),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX idx_faculties_code ON faculties(code);

-- Add FK from users to faculties (after faculties is created)
ALTER TABLE asp_net_users
ADD CONSTRAINT fk_users_faculty
FOREIGN KEY (faculty_id) REFERENCES faculties(id);

-- Programs (Licență, Master, Doctorat)
CREATE TABLE programs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    faculty_id UUID NOT NULL REFERENCES faculties(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    code VARCHAR(20) NOT NULL,
    duration INTEGER NOT NULL DEFAULT 3, -- ani
    type program_type NOT NULL DEFAULT 'Bachelor',
    total_credits INTEGER NOT NULL DEFAULT 180,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(faculty_id, code)
);

CREATE INDEX idx_programs_faculty ON programs(faculty_id);

-- Groups (Grupe de studenți)
CREATE TABLE groups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    program_id UUID NOT NULL REFERENCES programs(id) ON DELETE CASCADE,
    name VARCHAR(50) NOT NULL, -- ex: "3A", "2B"
    year INTEGER NOT NULL CHECK (year BETWEEN 1 AND 6),
    academic_year_id UUID REFERENCES academic_years(id),
    max_students INTEGER DEFAULT 30,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(program_id, name, year)
);

CREATE INDEX idx_groups_program ON groups(program_id);

-- Students
CREATE TABLE students (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES asp_net_users(id) ON DELETE CASCADE,
    group_id UUID NOT NULL REFERENCES groups(id),
    matriculation_number VARCHAR(20) NOT NULL UNIQUE,
    cnp VARCHAR(13), -- encrypted în producție
    status student_status NOT NULL DEFAULT 'Active',
    enrollment_year INTEGER NOT NULL,
    current_year INTEGER NOT NULL DEFAULT 1,
    scholarship_holder BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX idx_students_group ON students(group_id);
CREATE INDEX idx_students_status ON students(status);
CREATE INDEX idx_students_matriculation ON students(matriculation_number);

-- Professors
CREATE TABLE professors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES asp_net_users(id) ON DELETE CASCADE,
    faculty_id UUID NOT NULL REFERENCES faculties(id),
    department VARCHAR(200),
    title VARCHAR(100), -- Prof. Dr., Conf. Dr., etc.
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX idx_professors_faculty ON professors(faculty_id);

-- Courses
CREATE TABLE courses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    program_id UUID NOT NULL REFERENCES programs(id) ON DELETE CASCADE,
    professor_id UUID NOT NULL REFERENCES professors(id),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(20) NOT NULL,
    credits INTEGER NOT NULL DEFAULT 5,
    semester INTEGER NOT NULL CHECK (semester BETWEEN 1 AND 2),
    year INTEGER NOT NULL CHECK (year BETWEEN 1 AND 6),
    hours_lecture INTEGER DEFAULT 2,
    hours_lab INTEGER DEFAULT 2,
    hours_seminar INTEGER DEFAULT 0,
    is_mandatory BOOLEAN NOT NULL DEFAULT TRUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(program_id, code)
);

CREATE INDEX idx_courses_program ON courses(program_id);
CREATE INDEX idx_courses_professor ON courses(professor_id);

-- Grades
CREATE TABLE grades (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    professor_id UUID NOT NULL REFERENCES professors(id),
    value DECIMAL(4,2) NOT NULL CHECK (value BETWEEN 1 AND 10),
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    type grade_type NOT NULL DEFAULT 'Exam',
    status grade_status NOT NULL DEFAULT 'Pending',
    academic_year_id UUID REFERENCES academic_years(id),
    approved_by UUID REFERENCES asp_net_users(id),
    approved_at TIMESTAMPTZ,
    rejection_reason TEXT,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX idx_grades_student ON grades(student_id);
CREATE INDEX idx_grades_course ON grades(course_id);
CREATE INDEX idx_grades_status ON grades(status);
CREATE INDEX idx_grades_date ON grades(date);

-- Attendances
CREATE TABLE attendances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    professor_id UUID NOT NULL REFERENCES professors(id),
    date DATE NOT NULL,
    status attendance_status NOT NULL DEFAULT 'Present',
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(student_id, course_id, date)
);

CREATE INDEX idx_attendances_student ON attendances(student_id);
CREATE INDEX idx_attendances_course ON attendances(course_id);
CREATE INDEX idx_attendances_date ON attendances(date);

-- Schedule Entries (Orar)
CREATE TABLE schedule_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    group_id UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    day_of_week INTEGER NOT NULL CHECK (day_of_week BETWEEN 1 AND 7), -- 1=Luni
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    room VARCHAR(50),
    entry_type VARCHAR(20) NOT NULL DEFAULT 'Lecture', -- Lecture, Lab, Seminar
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_schedule_course ON schedule_entries(course_id);
CREATE INDEX idx_schedule_group ON schedule_entries(group_id);

-- Document Requests
CREATE TABLE document_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    type request_type NOT NULL,
    status request_status NOT NULL DEFAULT 'Pending',
    reason TEXT,
    processed_by UUID REFERENCES asp_net_users(id),
    processed_at TIMESTAMPTZ,
    document_url TEXT,
    registration_number VARCHAR(50),
    rejection_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX idx_requests_student ON document_requests(student_id);
CREATE INDEX idx_requests_status ON document_requests(status);

-- Notifications
CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    type notification_type NOT NULL DEFAULT 'Info',
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    link TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_user ON notifications(user_id);
CREATE INDEX idx_notifications_read ON notifications(is_read);
CREATE INDEX idx_notifications_created ON notifications(created_at DESC);

-- Audit Logs
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES asp_net_users(id),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID,
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    user_agent TEXT,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_user ON audit_logs(user_id);
CREATE INDEX idx_audit_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_timestamp ON audit_logs(timestamp DESC);

-- ============================================
-- ANALYTICS TABLES
-- ============================================

-- Daily Snapshots (pre-calculated pentru dashboard performance)
CREATE TABLE daily_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    date DATE NOT NULL,
    faculty_id UUID REFERENCES faculties(id),
    program_id UUID REFERENCES programs(id),
    total_students INTEGER NOT NULL DEFAULT 0,
    active_students INTEGER NOT NULL DEFAULT 0,
    average_grade DECIMAL(4,2),
    attendance_rate DECIMAL(5,2),
    grades_submitted INTEGER NOT NULL DEFAULT 0,
    grades_approved INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(date, faculty_id, program_id)
);

CREATE INDEX idx_daily_snapshots_date ON daily_snapshots(date);
CREATE INDEX idx_daily_snapshots_faculty ON daily_snapshots(faculty_id);

-- Grade Snapshots (per student, per semester)
CREATE TABLE grade_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    academic_year_id UUID REFERENCES academic_years(id),
    semester INTEGER NOT NULL CHECK (semester BETWEEN 1 AND 2),
    semester_average DECIMAL(4,2),
    cumulative_average DECIMAL(4,2),
    total_credits INTEGER NOT NULL DEFAULT 0,
    passed_credits INTEGER NOT NULL DEFAULT 0,
    failed_courses INTEGER NOT NULL DEFAULT 0,
    calculated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(student_id, academic_year_id, semester)
);

CREATE INDEX idx_grade_snapshots_student ON grade_snapshots(student_id);

-- Attendance Stats (pre-calculated per student/course)
CREATE TABLE attendance_stats (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    total_classes INTEGER NOT NULL DEFAULT 0,
    present_count INTEGER NOT NULL DEFAULT 0,
    absent_count INTEGER NOT NULL DEFAULT 0,
    excused_count INTEGER NOT NULL DEFAULT 0,
    attendance_rate DECIMAL(5,2),
    consecutive_absences INTEGER NOT NULL DEFAULT 0,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(student_id, course_id)
);

CREATE INDEX idx_attendance_stats_student ON attendance_stats(student_id);

-- Student Risk Scores (ML predictions)
CREATE TABLE student_risk_scores (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    overall_score INTEGER NOT NULL CHECK (overall_score BETWEEN 0 AND 100),
    level risk_level NOT NULL DEFAULT 'Low',
    grade_risk_factor DECIMAL(5,2),
    attendance_risk_factor DECIMAL(5,2),
    trend_risk_factor DECIMAL(5,2),
    engagement_risk_factor DECIMAL(5,2),
    risk_factors JSONB, -- array of specific risk reasons
    recommendations JSONB, -- array of suggested actions
    calculated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    reviewed_at TIMESTAMPTZ,
    reviewed_by UUID REFERENCES asp_net_users(id),
    notes TEXT
);

CREATE INDEX idx_risk_scores_student ON student_risk_scores(student_id);
CREATE INDEX idx_risk_scores_level ON student_risk_scores(level);
CREATE INDEX idx_risk_scores_score ON student_risk_scores(overall_score DESC);

-- Student Engagement Tracking
CREATE TABLE student_engagement (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    date DATE NOT NULL,
    login_count INTEGER NOT NULL DEFAULT 0,
    grades_viewed INTEGER NOT NULL DEFAULT 0,
    attendance_viewed INTEGER NOT NULL DEFAULT 0,
    documents_requested INTEGER NOT NULL DEFAULT 0,
    minutes_active INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(student_id, date)
);

CREATE INDEX idx_engagement_student ON student_engagement(student_id);
CREATE INDEX idx_engagement_date ON student_engagement(date);

-- Historical Metrics (pentru trend analysis pe ani)
CREATE TABLE historical_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    metric_type VARCHAR(50) NOT NULL, -- enrollment, graduation, dropout, etc.
    faculty_id UUID REFERENCES faculties(id),
    program_id UUID REFERENCES programs(id),
    academic_year_id UUID REFERENCES academic_years(id),
    value DECIMAL(10,2) NOT NULL,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_historical_faculty ON historical_metrics(faculty_id);
CREATE INDEX idx_historical_year ON historical_metrics(academic_year_id);
CREATE INDEX idx_historical_type ON historical_metrics(metric_type);

-- ============================================
-- VIEWS (pentru rapoarte)
-- ============================================

-- Student Overview View
CREATE OR REPLACE VIEW vw_student_overview AS
SELECT
    s.id,
    s.matriculation_number,
    u.first_name,
    u.last_name,
    u.email,
    s.status,
    s.current_year,
    g.name as group_name,
    p.name as program_name,
    p.type as program_type,
    f.name as faculty_name,
    s.enrollment_year,
    COALESCE(gs.cumulative_average, 0) as average,
    COALESCE(
        (SELECT AVG(ast.attendance_rate) FROM attendance_stats ast WHERE ast.student_id = s.id),
        0
    ) as attendance_rate
FROM students s
JOIN asp_net_users u ON s.user_id = u.id
JOIN groups g ON s.group_id = g.id
JOIN programs p ON g.program_id = p.id
JOIN faculties f ON p.faculty_id = f.id
LEFT JOIN grade_snapshots gs ON s.id = gs.student_id
    AND gs.calculated_at = (SELECT MAX(calculated_at) FROM grade_snapshots WHERE student_id = s.id);

-- Course Statistics View
CREATE OR REPLACE VIEW vw_course_statistics AS
SELECT
    c.id as course_id,
    c.name as course_name,
    c.code as course_code,
    p.name as program_name,
    f.name as faculty_name,
    CONCAT(u.first_name, ' ', u.last_name) as professor_name,
    COUNT(DISTINCT gr.student_id) as students_graded,
    AVG(gr.value) as average_grade,
    MIN(gr.value) as min_grade,
    MAX(gr.value) as max_grade,
    COUNT(CASE WHEN gr.value >= 5 THEN 1 END)::DECIMAL / NULLIF(COUNT(gr.id), 0) * 100 as pass_rate
FROM courses c
JOIN programs p ON c.program_id = p.id
JOIN faculties f ON p.faculty_id = f.id
JOIN professors pr ON c.professor_id = pr.id
JOIN asp_net_users u ON pr.user_id = u.id
LEFT JOIN grades gr ON c.id = gr.course_id AND gr.status = 'Approved'
GROUP BY c.id, c.name, c.code, p.name, f.name, u.first_name, u.last_name;

-- Faculty KPIs View
CREATE OR REPLACE VIEW vw_faculty_kpis AS
SELECT
    f.id as faculty_id,
    f.name as faculty_name,
    COUNT(DISTINCT s.id) as total_students,
    COUNT(DISTINCT CASE WHEN s.status = 'Active' THEN s.id END) as active_students,
    COUNT(DISTINCT pr.id) as total_professors,
    COUNT(DISTINCT c.id) as total_courses,
    AVG(gs.cumulative_average) as average_grade,
    AVG(ast.attendance_rate) as average_attendance
FROM faculties f
LEFT JOIN programs p ON p.faculty_id = f.id
LEFT JOIN groups g ON g.program_id = p.id
LEFT JOIN students s ON s.group_id = g.id
LEFT JOIN professors pr ON pr.faculty_id = f.id
LEFT JOIN courses c ON c.program_id = p.id
LEFT JOIN grade_snapshots gs ON gs.student_id = s.id
LEFT JOIN attendance_stats ast ON ast.student_id = s.id
GROUP BY f.id, f.name;

-- ============================================
-- FUNCTIONS
-- ============================================

-- Function: Calculate student risk score
CREATE OR REPLACE FUNCTION calculate_risk_score(p_student_id UUID)
RETURNS INTEGER AS $$
DECLARE
    v_grade_risk DECIMAL;
    v_attendance_risk DECIMAL;
    v_trend_risk DECIMAL;
    v_engagement_risk DECIMAL;
    v_total_score INTEGER;
    v_avg_grade DECIMAL;
    v_avg_attendance DECIMAL;
    v_consecutive_abs INTEGER;
BEGIN
    -- Get latest grade average
    SELECT cumulative_average INTO v_avg_grade
    FROM grade_snapshots
    WHERE student_id = p_student_id
    ORDER BY calculated_at DESC LIMIT 1;

    -- Get average attendance
    SELECT AVG(attendance_rate) INTO v_avg_attendance
    FROM attendance_stats
    WHERE student_id = p_student_id;

    -- Get max consecutive absences
    SELECT COALESCE(MAX(consecutive_absences), 0) INTO v_consecutive_abs
    FROM attendance_stats
    WHERE student_id = p_student_id;

    -- Calculate risk factors (0-25 each)
    v_grade_risk := CASE
        WHEN v_avg_grade IS NULL THEN 10
        WHEN v_avg_grade < 5 THEN 25
        WHEN v_avg_grade < 6 THEN 20
        WHEN v_avg_grade < 7 THEN 10
        ELSE 0
    END;

    v_attendance_risk := CASE
        WHEN v_avg_attendance IS NULL THEN 10
        WHEN v_avg_attendance < 50 THEN 25
        WHEN v_avg_attendance < 70 THEN 20
        WHEN v_avg_attendance < 85 THEN 10
        ELSE 0
    END;

    v_trend_risk := CASE
        WHEN v_consecutive_abs >= 5 THEN 25
        WHEN v_consecutive_abs >= 3 THEN 15
        WHEN v_consecutive_abs >= 2 THEN 5
        ELSE 0
    END;

    -- Engagement risk (placeholder - would need more data)
    v_engagement_risk := 0;

    v_total_score := LEAST(100, (v_grade_risk + v_attendance_risk + v_trend_risk + v_engagement_risk)::INTEGER);

    RETURN v_total_score;
END;
$$ LANGUAGE plpgsql;

-- Function: Update attendance stats
CREATE OR REPLACE FUNCTION update_attendance_stats()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO attendance_stats (student_id, course_id, total_classes, present_count, absent_count, excused_count, attendance_rate, consecutive_absences)
    SELECT
        NEW.student_id,
        NEW.course_id,
        COUNT(*),
        COUNT(CASE WHEN status = 'Present' THEN 1 END),
        COUNT(CASE WHEN status = 'Absent' THEN 1 END),
        COUNT(CASE WHEN status = 'Excused' THEN 1 END),
        COUNT(CASE WHEN status = 'Present' THEN 1 END)::DECIMAL / NULLIF(COUNT(*), 0) * 100,
        0 -- Would need more complex logic for consecutive
    FROM attendances
    WHERE student_id = NEW.student_id AND course_id = NEW.course_id
    GROUP BY student_id, course_id
    ON CONFLICT (student_id, course_id)
    DO UPDATE SET
        total_classes = EXCLUDED.total_classes,
        present_count = EXCLUDED.present_count,
        absent_count = EXCLUDED.absent_count,
        excused_count = EXCLUDED.excused_count,
        attendance_rate = EXCLUDED.attendance_rate,
        last_updated = NOW();

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger: Auto-update attendance stats
CREATE TRIGGER trg_update_attendance_stats
AFTER INSERT OR UPDATE ON attendances
FOR EACH ROW
EXECUTE FUNCTION update_attendance_stats();

-- ============================================
-- SEED DATA (Optional - for development)
-- ============================================

-- Insert default roles
INSERT INTO asp_net_roles (id, name, normalized_name) VALUES
    (gen_random_uuid(), 'Admin', 'ADMIN'),
    (gen_random_uuid(), 'Student', 'STUDENT'),
    (gen_random_uuid(), 'Professor', 'PROFESSOR'),
    (gen_random_uuid(), 'Secretary', 'SECRETARY'),
    (gen_random_uuid(), 'Dean', 'DEAN'),
    (gen_random_uuid(), 'Rector', 'RECTOR');

-- Insert current academic year
INSERT INTO academic_years (id, name, start_date, end_date, is_current) VALUES
    (gen_random_uuid(), '2024-2025', '2024-10-01', '2025-07-31', TRUE);

-- ============================================
-- PERMISSIONS (RLS - Optional pentru Supabase)
-- ============================================

-- Enable RLS pe tabelele principale (opțional)
-- ALTER TABLE students ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE grades ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE attendances ENABLE ROW LEVEL SECURITY;

-- Policy examples (ar fi definite în funcție de necesități)
-- CREATE POLICY "Students can view own data" ON students
--     FOR SELECT USING (user_id = auth.uid());

-- ============================================
-- INDEXES pentru performance
-- ============================================

-- Composite indexes pentru queries frecvente
CREATE INDEX idx_grades_student_course ON grades(student_id, course_id);
CREATE INDEX idx_grades_status_date ON grades(status, date);
CREATE INDEX idx_attendances_student_course_date ON attendances(student_id, course_id, date);
CREATE INDEX idx_notifications_user_read_created ON notifications(user_id, is_read, created_at DESC);

-- ============================================
-- COMMENTS pentru documentație
-- ============================================

COMMENT ON TABLE students IS 'Studenți înmatriculați în universitate';
COMMENT ON TABLE grades IS 'Note acordate studenților - necesită aprobare decan';
COMMENT ON TABLE student_risk_scores IS 'Scoruri de risc calculate automat pentru early warning system';
COMMENT ON TABLE daily_snapshots IS 'Snapshot-uri zilnice pentru dashboard performance și trend analysis';
COMMENT ON COLUMN student_risk_scores.overall_score IS 'Scor 0-100: Low(0-30), Medium(31-60), High(61-80), Critical(81-100)';
