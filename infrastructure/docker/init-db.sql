-- Create schemas
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS core;

-- Create service users (for local dev environments)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'devspace_auth') THEN
        CREATE ROLE devspace_auth WITH LOGIN PASSWORD 'devspace_auth_pwd_123';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'devspace_core') THEN
        CREATE ROLE devspace_core WITH LOGIN PASSWORD 'devspace_core_pwd_123';
    END IF;
END
$$;

-- Grant schema-level permissions and set ownership
GRANT ALL ON SCHEMA auth TO devspace_auth;
GRANT ALL ON SCHEMA core TO devspace_core;

ALTER SCHEMA auth OWNER TO devspace_auth;
ALTER SCHEMA core OWNER TO devspace_core;
