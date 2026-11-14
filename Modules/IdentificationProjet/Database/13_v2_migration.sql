-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 2
-- 13_v2_migration.sql : génère une grille pour les fiches déjà au MPCE
-- (statut TRANSMISE_DIP) avant le passage en v2. Sans effet sur une base neuve.
-- =============================================================================
BEGIN
    FOR f IN (SELECT f.id_fiche FROM tb_ip_fiche f
               WHERE f.statut = 'TRANSMISE_DIP'
                 AND NOT EXISTS (SELECT 1 FROM tb_ip_grille g
                                  WHERE g.id_fiche = f.id_fiche AND g.statut <> 'APPROUVEE')) LOOP
        pkg_ip_identification.generer_grille(f.id_fiche);
    END LOOP;
    COMMIT;
END;
/
