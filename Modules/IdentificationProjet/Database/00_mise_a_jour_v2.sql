-- =============================================================================
-- SIIP - Module "Identification projet" : MISE À JOUR v1 -> v2 (données conservées)
-- À exécuter une seule fois sur un schéma où 00_installer.sql (v1) a déjà été lancé.
--   SQL> @00_mise_a_jour_v2.sql
-- =============================================================================
SET DEFINE OFF
WHENEVER SQLERROR EXIT SQL.SQLCODE
@@06_v2_types.sql
@@07_v2_tables.sql
@@08_v2_types_corps.sql
@@09_v2_vues.sql
@@10_v2_package.sql
@@11_donnees_localisation.sql
@@12_v2_donnees.sql
@@13_v2_migration.sql
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
PROMPT Module Identification projet mis à jour (v2).
