-- ============================================
-- SMU 3.0 - Seed Data
-- Demo accounts and test data
-- Version: 2.0
-- ============================================

-- Note: Passwords are hashed using ASP.NET Identity (PBKDF2)
-- For demo, we'll set placeholder hashes - actual passwords will be set via the app
-- Default password for all demo accounts: "Demo123!"

-- ============================================
-- FACULTIES
-- ============================================

INSERT INTO faculties (id, name, code, description) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Facultatea de Informatică și Comunicații', 'FIC', 'Facultatea care pregătește specialiști în domeniul IT și comunicații'),
    ('22222222-2222-2222-2222-222222222222', 'Facultatea de Științe Economice', 'FSE', 'Facultatea de business și economie'),
    ('33333333-3333-3333-3333-333333333333', 'Facultatea de Drept', 'FD', 'Facultatea de studii juridice');

-- ============================================
-- PROGRAMS
-- ============================================

INSERT INTO programs (id, faculty_id, name, code, duration, type, total_credits) VALUES
    -- FIC Programs
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Informatică Aplicată', 'IA', 3, 'Bachelor', 180),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '11111111-1111-1111-1111-111111111111', 'Securitate Cibernetică', 'SC', 3, 'Bachelor', 180),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', '11111111-1111-1111-1111-111111111111', 'Inteligență Artificială', 'IAI', 2, 'Master', 120),
    -- FSE Programs
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', '22222222-2222-2222-2222-222222222222', 'Administrarea Afacerilor', 'AA', 3, 'Bachelor', 180),
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', '22222222-2222-2222-2222-222222222222', 'Marketing', 'MKT', 3, 'Bachelor', 180),
    -- FD Programs
    ('ffffffff-ffff-ffff-ffff-ffffffffffff', '33333333-3333-3333-3333-333333333333', 'Drept', 'DR', 4, 'Bachelor', 240);

-- ============================================
-- GROUPS
-- ============================================

INSERT INTO groups (id, program_id, name, year, max_students) VALUES
    -- IA Groups
    ('a1111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '1A', 1, 30),
    ('a2222222-2222-2222-2222-222222222222', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '1B', 1, 30),
    ('a3333333-3333-3333-3333-333333333333', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2A', 2, 30),
    ('a4444444-4444-4444-4444-444444444444', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '3A', 3, 25),
    -- SC Groups
    ('b1111111-1111-1111-1111-111111111111', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '1A', 1, 30),
    ('b2222222-2222-2222-2222-222222222222', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2A', 2, 30),
    -- AA Groups
    ('c1111111-1111-1111-1111-111111111111', 'dddddddd-dddd-dddd-dddd-dddddddddddd', '1A', 1, 35),
    ('c2222222-2222-2222-2222-222222222222', 'dddddddd-dddd-dddd-dddd-dddddddddddd', '2A', 2, 35);

-- ============================================
-- DEMO USERS (ASP.NET Identity format)
-- ============================================

-- Admin User
INSERT INTO asp_net_users (id, user_name, normalized_user_name, email, normalized_email, email_confirmed, password_hash, security_stamp, first_name, last_name, role, is_active, created_at) VALUES
    ('00000000-0000-0000-0000-000000000001', 'admin@rau.ro', 'ADMIN@RAU.RO', 'admin@rau.ro', 'ADMIN@RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_admin', gen_random_uuid()::text,
     'Admin', 'System', 'Admin', TRUE, NOW());

-- Rector User
INSERT INTO asp_net_users (id, user_name, normalized_user_name, email, normalized_email, email_confirmed, password_hash, security_stamp, first_name, last_name, role, is_active, created_at) VALUES
    ('00000000-0000-0000-0000-000000000002', 'rector@rau.ro', 'RECTOR@RAU.RO', 'rector@rau.ro', 'RECTOR@RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_rector', gen_random_uuid()::text,
     'Ion', 'Popescu', 'Rector', TRUE, NOW());

-- Dean Users (one per faculty)
INSERT INTO asp_net_users (id, user_name, normalized_user_name, email, normalized_email, email_confirmed, password_hash, security_stamp, first_name, last_name, role, faculty_id, is_active, created_at) VALUES
    ('00000000-0000-0000-0000-000000000003', 'dean.fic@rau.ro', 'DEAN.FIC@RAU.RO', 'dean.fic@rau.ro', 'DEAN.FIC@RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_dean1', gen_random_uuid()::text,
     'Maria', 'Ionescu', 'Dean', '11111111-1111-1111-1111-111111111111', TRUE, NOW()),
    ('00000000-0000-0000-0000-000000000004', 'dean.fse@rau.ro', 'DEAN.FSE@RAU.RO', 'dean.fse@rau.ro', 'DEAN.FSE@RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_dean2', gen_random_uuid()::text,
     'Andrei', 'Georgescu', 'Dean', '22222222-2222-2222-2222-222222222222', TRUE, NOW());

-- Update faculties with dean references
UPDATE faculties SET dean_id = '00000000-0000-0000-0000-000000000003' WHERE id = '11111111-1111-1111-1111-111111111111';
UPDATE faculties SET dean_id = '00000000-0000-0000-0000-000000000004' WHERE id = '22222222-2222-2222-2222-222222222222';

-- Secretary Users
INSERT INTO asp_net_users (id, user_name, normalized_user_name, email, normalized_email, email_confirmed, password_hash, security_stamp, first_name, last_name, role, faculty_id, is_active, created_at) VALUES
    ('00000000-0000-0000-0000-000000000005', 'secretary.fic@rau.ro', 'SECRETARY.FIC@RAU.RO', 'secretary.fic@rau.ro', 'SECRETARY.FIC@RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_sec1', gen_random_uuid()::text,
     'Elena', 'Vasilescu', 'Secretary', '11111111-1111-1111-1111-111111111111', TRUE, NOW()),
    ('00000000-0000-0000-0000-000000000006', 'secretary.fse@rau.ro', 'SECRETARY.FSE@RAU.RO', 'secretary.fse@rau.ro', 'SECRETARY.FSE@RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_sec2', gen_random_uuid()::text,
     'Ana', 'Munteanu', 'Secretary', '22222222-2222-2222-2222-222222222222', TRUE, NOW());

-- Professor Users
INSERT INTO asp_net_users (id, user_name, normalized_user_name, email, normalized_email, email_confirmed, password_hash, security_stamp, first_name, last_name, role, faculty_id, is_active, created_at) VALUES
    ('00000000-0000-0000-0000-000000000010', 'prof.dumitru@rau.ro', 'PROF.DUMITRU@RAU.RO', 'prof.dumitru@rau.ro', 'PROF.DUMITRU@RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_prof1', gen_random_uuid()::text,
     'Alexandru', 'Dumitru', 'Professor', '11111111-1111-1111-1111-111111111111', TRUE, NOW()),
    ('00000000-0000-0000-0000-000000000011', 'prof.stanescu@rau.ro', 'PROF.STANESCU@RAU.RO', 'prof.stanescu@rau.ro', 'PROF.STANESCU@RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_prof2', gen_random_uuid()::text,
     'Mihai', 'Stănescu', 'Professor', '11111111-1111-1111-1111-111111111111', TRUE, NOW()),
    ('00000000-0000-0000-0000-000000000012', 'prof.popa@rau.ro', 'PROF.POPA@RAU.RO', 'prof.popa@rau.ro', 'PROF.POPA@RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_prof3', gen_random_uuid()::text,
     'Cristina', 'Popa', 'Professor', '22222222-2222-2222-2222-222222222222', TRUE, NOW());

-- Professor entries
INSERT INTO professors (id, user_id, faculty_id, department, title) VALUES
    ('p0000000-0000-0000-0000-000000000010', '00000000-0000-0000-0000-000000000010', '11111111-1111-1111-1111-111111111111', 'Departamentul de Informatică', 'Prof. Dr.'),
    ('p0000000-0000-0000-0000-000000000011', '00000000-0000-0000-0000-000000000011', '11111111-1111-1111-1111-111111111111', 'Departamentul de Informatică', 'Conf. Dr.'),
    ('p0000000-0000-0000-0000-000000000012', '00000000-0000-0000-0000-000000000012', '22222222-2222-2222-2222-222222222222', 'Departamentul de Management', 'Lect. Dr.');

-- Student Users
INSERT INTO asp_net_users (id, user_name, normalized_user_name, email, normalized_email, email_confirmed, password_hash, security_stamp, first_name, last_name, role, faculty_id, is_active, created_at) VALUES
    ('00000000-0000-0000-0000-000000000020', 'student1@stud.rau.ro', 'STUDENT1@STUD.RAU.RO', 'student1@stud.rau.ro', 'STUDENT1@STUD.RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_stud1', gen_random_uuid()::text,
     'Andrei', 'Marinescu', 'Student', '11111111-1111-1111-1111-111111111111', TRUE, NOW()),
    ('00000000-0000-0000-0000-000000000021', 'student2@stud.rau.ro', 'STUDENT2@STUD.RAU.RO', 'student2@stud.rau.ro', 'STUDENT2@STUD.RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_stud2', gen_random_uuid()::text,
     'Elena', 'Radu', 'Student', '11111111-1111-1111-1111-111111111111', TRUE, NOW()),
    ('00000000-0000-0000-0000-000000000022', 'student3@stud.rau.ro', 'STUDENT3@STUD.RAU.RO', 'student3@stud.rau.ro', 'STUDENT3@STUD.RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_stud3', gen_random_uuid()::text,
     'Mihai', 'Stoica', 'Student', '11111111-1111-1111-1111-111111111111', TRUE, NOW()),
    ('00000000-0000-0000-0000-000000000023', 'student4@stud.rau.ro', 'STUDENT4@STUD.RAU.RO', 'student4@stud.rau.ro', 'STUDENT4@STUD.RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_stud4', gen_random_uuid()::text,
     'Ioana', 'Dumitrescu', 'Student', '22222222-2222-2222-2222-222222222222', TRUE, NOW()),
    ('00000000-0000-0000-0000-000000000024', 'student5@stud.rau.ro', 'STUDENT5@STUD.RAU.RO', 'student5@stud.rau.ro', 'STUDENT5@STUD.RAU.RO', TRUE,
     'AQAAAAIAAYagAAAAEDemo_placeholder_hash_stud5', gen_random_uuid()::text,
     'Alexandru', 'Constantinescu', 'Student', '11111111-1111-1111-1111-111111111111', TRUE, NOW());

-- Student entries
INSERT INTO students (id, user_id, group_id, matriculation_number, status, enrollment_year, current_year) VALUES
    ('s0000000-0000-0000-0000-000000000020', '00000000-0000-0000-0000-000000000020', 'a1111111-1111-1111-1111-111111111111', 'IA-2024-001', 'Active', 2024, 1),
    ('s0000000-0000-0000-0000-000000000021', '00000000-0000-0000-0000-000000000021', 'a1111111-1111-1111-1111-111111111111', 'IA-2024-002', 'Active', 2024, 1),
    ('s0000000-0000-0000-0000-000000000022', '00000000-0000-0000-0000-000000000022', 'a3333333-3333-3333-3333-333333333333', 'IA-2022-015', 'Active', 2022, 2),
    ('s0000000-0000-0000-0000-000000000023', '00000000-0000-0000-0000-000000000023', 'c1111111-1111-1111-1111-111111111111', 'AA-2024-001', 'Active', 2024, 1),
    ('s0000000-0000-0000-0000-000000000024', '00000000-0000-0000-0000-000000000024', 'b1111111-1111-1111-1111-111111111111', 'SC-2024-001', 'Active', 2024, 1);

-- ============================================
-- COURSES
-- ============================================

INSERT INTO courses (id, program_id, professor_id, name, code, credits, semester, year, hours_lecture, hours_lab, is_mandatory) VALUES
    -- IA Year 1 Courses
    ('c0000000-0000-0000-0000-000000000001', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'p0000000-0000-0000-0000-000000000010', 'Programare Orientată pe Obiecte', 'POO', 6, 1, 1, 2, 2, TRUE),
    ('c0000000-0000-0000-0000-000000000002', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'p0000000-0000-0000-0000-000000000010', 'Structuri de Date', 'SD', 5, 1, 1, 2, 2, TRUE),
    ('c0000000-0000-0000-0000-000000000003', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'p0000000-0000-0000-0000-000000000011', 'Baze de Date', 'BD', 6, 2, 1, 2, 2, TRUE),
    ('c0000000-0000-0000-0000-000000000004', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'p0000000-0000-0000-0000-000000000011', 'Rețele de Calculatoare', 'RC', 5, 2, 1, 2, 2, TRUE),
    -- IA Year 2 Courses
    ('c0000000-0000-0000-0000-000000000005', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'p0000000-0000-0000-0000-000000000010', 'Dezvoltare Web', 'DW', 6, 1, 2, 2, 2, TRUE),
    ('c0000000-0000-0000-0000-000000000006', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'p0000000-0000-0000-0000-000000000011', 'Inginerie Software', 'IS', 5, 1, 2, 2, 2, TRUE),
    -- SC Year 1 Courses
    ('c0000000-0000-0000-0000-000000000007', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'p0000000-0000-0000-0000-000000000011', 'Securitate Informatică', 'SI', 6, 1, 1, 2, 2, TRUE),
    -- AA Year 1 Courses
    ('c0000000-0000-0000-0000-000000000008', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 'p0000000-0000-0000-0000-000000000012', 'Management General', 'MG', 5, 1, 1, 3, 0, TRUE),
    ('c0000000-0000-0000-0000-000000000009', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 'p0000000-0000-0000-0000-000000000012', 'Economie', 'ECO', 5, 1, 1, 3, 0, TRUE);

-- ============================================
-- SAMPLE GRADES
-- ============================================

INSERT INTO grades (id, student_id, course_id, professor_id, value, date, type, status) VALUES
    -- Student 1 grades
    ('g0000000-0000-0000-0000-000000000001', 's0000000-0000-0000-0000-000000000020', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', 8.50, '2024-11-15', 'Lab', 'Approved'),
    ('g0000000-0000-0000-0000-000000000002', 's0000000-0000-0000-0000-000000000020', 'c0000000-0000-0000-0000-000000000002', 'p0000000-0000-0000-0000-000000000010', 7.00, '2024-11-20', 'Lab', 'Approved'),
    ('g0000000-0000-0000-0000-000000000003', 's0000000-0000-0000-0000-000000000020', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', 9.00, '2024-11-28', 'Seminar', 'Pending'),
    -- Student 2 grades
    ('g0000000-0000-0000-0000-000000000004', 's0000000-0000-0000-0000-000000000021', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', 6.50, '2024-11-15', 'Lab', 'Approved'),
    ('g0000000-0000-0000-0000-000000000005', 's0000000-0000-0000-0000-000000000021', 'c0000000-0000-0000-0000-000000000002', 'p0000000-0000-0000-0000-000000000010', 5.50, '2024-11-20', 'Lab', 'Approved'),
    -- Student 3 grades (Year 2)
    ('g0000000-0000-0000-0000-000000000006', 's0000000-0000-0000-0000-000000000022', 'c0000000-0000-0000-0000-000000000005', 'p0000000-0000-0000-0000-000000000010', 9.50, '2024-11-10', 'Project', 'Approved'),
    ('g0000000-0000-0000-0000-000000000007', 's0000000-0000-0000-0000-000000000022', 'c0000000-0000-0000-0000-000000000006', 'p0000000-0000-0000-0000-000000000011', 8.00, '2024-11-25', 'Lab', 'Pending');

-- ============================================
-- SAMPLE ATTENDANCES
-- ============================================

INSERT INTO attendances (student_id, course_id, professor_id, date, status) VALUES
    -- Student 1 attendances
    ('s0000000-0000-0000-0000-000000000020', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', '2024-11-01', 'Present'),
    ('s0000000-0000-0000-0000-000000000020', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', '2024-11-08', 'Present'),
    ('s0000000-0000-0000-0000-000000000020', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', '2024-11-15', 'Absent'),
    ('s0000000-0000-0000-0000-000000000020', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', '2024-11-22', 'Present'),
    ('s0000000-0000-0000-0000-000000000020', 'c0000000-0000-0000-0000-000000000002', 'p0000000-0000-0000-0000-000000000010', '2024-11-05', 'Present'),
    ('s0000000-0000-0000-0000-000000000020', 'c0000000-0000-0000-0000-000000000002', 'p0000000-0000-0000-0000-000000000010', '2024-11-12', 'Present'),
    -- Student 2 attendances
    ('s0000000-0000-0000-0000-000000000021', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', '2024-11-01', 'Present'),
    ('s0000000-0000-0000-0000-000000000021', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', '2024-11-08', 'Absent'),
    ('s0000000-0000-0000-0000-000000000021', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', '2024-11-15', 'Excused'),
    ('s0000000-0000-0000-0000-000000000021', 'c0000000-0000-0000-0000-000000000001', 'p0000000-0000-0000-0000-000000000010', '2024-11-22', 'Present');

-- ============================================
-- SAMPLE NOTIFICATIONS
-- ============================================

INSERT INTO notifications (user_id, title, message, type, is_read, created_at) VALUES
    ('00000000-0000-0000-0000-000000000020', 'Notă nouă înregistrată', 'Ați primit nota 8.50 la POO - Laborator', 'GradeAdded', FALSE, NOW() - INTERVAL '2 days'),
    ('00000000-0000-0000-0000-000000000020', 'Prezență marcată', 'A fost înregistrată absența din 15.11.2024 la POO', 'AttendanceMarked', FALSE, NOW() - INTERVAL '1 day'),
    ('00000000-0000-0000-0000-000000000003', 'Note în așteptare', 'Există 2 note care necesită aprobarea dumneavoastră', 'Warning', FALSE, NOW()),
    ('00000000-0000-0000-0000-000000000010', 'Bun venit!', 'Bine ați venit în SMU 3.0! Sistemul este pregătit pentru utilizare.', 'Info', TRUE, NOW() - INTERVAL '7 days');

-- ============================================
-- DEMO ACCOUNT CREDENTIALS
-- ============================================
/*
Demo accounts for testing (password for all: Demo123!)

ADMIN:
  Email: admin@rau.ro

RECTOR:
  Email: rector@rau.ro

DEANI:
  Email: dean.fic@rau.ro (Facultatea de Informatică)
  Email: dean.fse@rau.ro (Facultatea de Economie)

SECRETARIAT:
  Email: secretary.fic@rau.ro
  Email: secretary.fse@rau.ro

PROFESORI:
  Email: prof.dumitru@rau.ro
  Email: prof.stanescu@rau.ro
  Email: prof.popa@rau.ro

STUDENȚI:
  Email: student1@stud.rau.ro (IA, An 1)
  Email: student2@stud.rau.ro (IA, An 1)
  Email: student3@stud.rau.ro (IA, An 2)
  Email: student4@stud.rau.ro (AA, An 1)
  Email: student5@stud.rau.ro (SC, An 1)
*/
