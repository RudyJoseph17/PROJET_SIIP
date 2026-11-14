-- =============================================================================
-- SIIP - Module "Identification projet"
-- 04_vues.sql : vues relationnelles sur les tables objet (lues par EF Core)
-- Les écritures passent exclusivement par le package PKG_IP_IDENTIFICATION.
-- =============================================================================

CREATE OR REPLACE VIEW v_ip_fiche AS
SELECT CAST(f.id_fiche AS NUMBER(10))         AS id_fiche,
       f.code_fiche,
       f.contact.nom_organisation              AS nom_organisation,
       f.contact.nom_contact                   AS nom_contact,
       f.contact.telephone                     AS telephone,
       f.contact.courriel                      AS courriel,
       f.titre_projet,
       f.description_probleme,
       f.donnees_probleme,
       f.consequences,
       f.justification_choix,
       f.conclusions,
       f.statut,
       f.promoteur_id,
       f.promoteur_nom,
       f.ministere,
       f.date_creation,
       f.date_modification,
       f.date_soumission,
       CAST(f.etape_complete(1) AS NUMBER(1)) AS etape1,
       CAST(f.etape_complete(2) AS NUMBER(1)) AS etape2,
       CAST(f.etape_complete(3) AS NUMBER(1)) AS etape3,
       CAST(f.etape_complete(4) AS NUMBER(1)) AS etape4,
       CAST(f.etape_complete(5) AS NUMBER(1)) AS etape5,
       CAST(f.progression()     AS NUMBER(3)) AS progression,
       CAST(f.est_modifiable()  AS NUMBER(1)) AS est_modifiable,
       CAST((SELECT COUNT(*) FROM tb_ip_idee_possible i
              WHERE i.id_fiche = f.id_fiche) AS NUMBER(10)) AS nb_idees,
       CAST((SELECT COUNT(*) FROM tb_ip_annexe a
              WHERE a.id_fiche = f.id_fiche) AS NUMBER(10)) AS nb_annexes,
       (SELECT i.intitule FROM tb_ip_idee_possible i
         WHERE i.id_fiche = f.id_fiche AND i.est_meilleure = 1) AS meilleure_idee
  FROM tb_ip_fiche f;

CREATE OR REPLACE VIEW v_ip_idee_possible AS
SELECT CAST(i.id_idee  AS NUMBER(10))        AS id_idee,
       CAST(i.id_fiche AS NUMBER(10))        AS id_fiche,
       CAST(i.ordre    AS NUMBER(5))         AS ordre,
       i.intitule,
       i.description,
       i.legitimite,
       i.pertinence,
       i.viabilite,
       CAST(i.est_meilleure  AS NUMBER(1))   AS est_meilleure,
       CAST(i.est_complete() AS NUMBER(1))   AS est_complete
  FROM tb_ip_idee_possible i;

-- Le contenu (BLOB) n'est pas exposé : il est lu à la demande au téléchargement
CREATE OR REPLACE VIEW v_ip_annexe AS
SELECT CAST(a.id_annexe AS NUMBER(10)) AS id_annexe,
       CAST(a.id_fiche  AS NUMBER(10)) AS id_fiche,
       a.nom_fichier,
       a.type_mime,
       CAST(a.taille AS NUMBER(12))    AS taille,
       a.description,
       a.date_ajout,
       a.ajoute_par
  FROM tb_ip_annexe a;

CREATE OR REPLACE VIEW v_ip_historique AS
SELECT CAST(h.id_historique AS NUMBER(10)) AS id_historique,
       CAST(h.id_fiche      AS NUMBER(10)) AS id_fiche,
       h.action,
       h.ancien_statut,
       h.nouveau_statut,
       h.commentaire,
       h.acteur_id,
       h.acteur_nom,
       h.date_action
  FROM tb_ip_historique h;
