-- AQUILLA TEXTILES: add email support to existing user accounts.
-- Existing users are preserved; their email fields remain NULL until updated separately.

ALTER TABLE public."Users"
ADD COLUMN IF NOT EXISTS "Email" character varying(254);

ALTER TABLE public."Users"
ADD COLUMN IF NOT EXISTS "NormalizedEmail" character varying(254);

-- PostgreSQL permits multiple NULL values in a unique index, so existing users
-- without email addresses do not conflict with one another.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_NormalizedEmail"
ON public."Users" ("NormalizedEmail");
