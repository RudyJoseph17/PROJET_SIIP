-- =============================================================================
-- SIIP - Module "Identification projet" : INSTALLATION COMPLÈTE (base neuve)
-- SQL*Plus / SQLcl / SQL Developer (F5), connecté au schéma applicatif :
--   SQL> @00_installer.sql
-- Base existante : @00_mise_a_jour_v2.sql (depuis v1) puis @00_mise_a_jour_v3.sql (depuis v2)
-- =============================================================================
SET DEFINE OFF
WHENEVER SQLERROR EXIT SQL.SQLCODE
PROMPT === Version 1 : fiche d'identification
@@01_types.sql
@@02_tables.sql
@@03_types_corps.sql
@@04_vues.sql
@@05_package.sql
PROMPT === Version 2 : grille d'analyse du MPCE
@@06_v2_types.sql
@@07_v2_tables.sql
@@08_v2_types_corps.sql
@@09_v2_vues.sql
@@10_v2_package.sql
@@11_donnees_localisation.sql
@@12_v2_donnees.sql
@@13_v2_migration.sql
PROMPT === Version 3 : préfaisabilité, faisabilité, études de projet
@@14_v3_types.sql
@@15_v3_tables.sql
@@16_v3_types_corps.sql
@@17_v3_vues.sql
@@18_v3_package_etude.sql
@@19_v3_migration.sql
PROMPT === Vérification : aucun objet invalide
DECLARE
    v_liste VARCHAR2(4000);
BEGIN
    DBMS_UTILITY.COMPILE_SCHEMA(schema => USER, compile_all => FALSE);
    SELECT LISTAGG(object_type || ' ' || object_name, ', ') WITHIN GROUP (ORDER BY object_name)
      INTO v_liste
      FROM user_objects
     WHERE status = 'INVALID'
       AND (object_name LIKE '%\_IP\_%' ESCAPE '\' OR object_name LIKE 'PKG\_IP\_%' ESCAPE '\');
    IF v_liste IS NOT NULL THEN
        RAISE_APPLICATION_ERROR(-20999, 'Objets invalides : ' || v_liste
            || '. Consultez USER_ERRORS pour le détail.');
    END IF;
END;
/
PROMPT Module Identification projet installé (v3).
