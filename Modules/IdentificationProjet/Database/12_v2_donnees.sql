-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 2
-- 12_v2_donnees.sql : questions « Généralités » de la grille d'analyse
-- Rejouable (MERGE). Pour retirer une question des futures grilles : actif = 0.
-- =============================================================================
SET DEFINE OFF
MERGE INTO tb_ip_question q
USING (
    SELECT  1 numero, 'Les 4 sections du document sont présentes' libelle FROM dual UNION ALL
    SELECT  2, 'Le titre du projet est proposé suivant la formule AOL' FROM dual UNION ALL
    SELECT  3, 'Le problème à résoudre est identifié' FROM dual UNION ALL
    SELECT  4, 'Des données quantitatives et qualitatives sont fournies pour démontrer l''existence du problème' FROM dual UNION ALL
    SELECT  5, 'Les idées pour résoudre le problème sont identifiées' FROM dual UNION ALL
    SELECT  6, 'La cohérence de chaque idée avec les objectifs globaux, régionaux et sectoriels est décrite' FROM dual UNION ALL
    SELECT  7, 'Les facteurs de soutien et de blocage de chaque idée sont présentés' FROM dual UNION ALL
    SELECT  8, 'La meilleure idée pour résoudre le problème est choisie' FROM dual UNION ALL
    SELECT  9, 'Les principales conclusions et les mesures à prendre pour la meilleure idée sont présentées' FROM dual UNION ALL
    SELECT 10, 'L''arbre des problèmes est présenté' FROM dual UNION ALL
    SELECT 11, 'L''arbre des objectifs est présenté' FROM dual
) s ON (q.numero = s.numero)
WHEN MATCHED THEN UPDATE SET q.libelle = s.libelle
WHEN NOT MATCHED THEN INSERT VALUES (t_ip_question(s.numero, s.libelle, 1));
COMMIT;
