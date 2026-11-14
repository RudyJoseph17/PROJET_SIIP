-- =============================================================================
-- SIIP - Module "Identification projet" : MISE À JOUR v2 -> v3 (données conservées)
-- À exécuter une seule fois sur un schéma déjà en v2 (grille d'analyse installée).
--   SQL> @00_mise_a_jour_v3.sql
-- =============================================================================
SET DEFINE OFF
WHENEVER SQLERROR EXIT SQL.SQLCODE
@@14_v3_types.sql
@@15_v3_tables.sql
@@16_v3_types_corps.sql
@@17_v3_vues.sql
@@18_v3_package_etude.sql
-- Le package de la fiche est mis à jour (historique + ouverture de la préfaisabilité)
@@10_v2_package.sql
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
PROMPT Module Identification projet mis à jour (v3).
