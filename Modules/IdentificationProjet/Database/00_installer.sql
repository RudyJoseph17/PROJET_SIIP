-- =============================================================================
-- SIIP - Module "Identification projet" : installation complète
-- Exécuter avec SQL*Plus / SQLcl connecté au schéma applicatif (ex. RUDYADM) :
--   SQL> @00_installer.sql
-- =============================================================================
SET DEFINE OFF
WHENEVER SQLERROR EXIT SQL.SQLCODE
@@01_types.sql
@@02_tables.sql
@@03_types_corps.sql
@@04_vues.sql
@@05_package.sql
SHOW ERRORS
PROMPT Module Identification projet installé.
