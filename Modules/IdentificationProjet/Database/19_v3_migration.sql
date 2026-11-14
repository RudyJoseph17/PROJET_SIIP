-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 3
-- 19_v3_migration.sql : ouvre la phase de préfaisabilité des fiches déjà validées
-- avant le passage en v3. Sans effet sur une base neuve.
-- =============================================================================
BEGIN
    FOR f IN (SELECT f.id_fiche FROM tb_ip_fiche f
               WHERE f.statut = 'APPROUVEE_DIP'
                 AND NOT EXISTS (SELECT 1 FROM tb_ip_etude e WHERE e.id_fiche = f.id_fiche)) LOOP
        pkg_ip_etude.ouvrir_phase(f.id_fiche, 'PREFAISABILITE');
    END LOOP;
    COMMIT;
END;
/
