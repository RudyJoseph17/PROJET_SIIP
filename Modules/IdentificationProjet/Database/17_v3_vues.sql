-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 3
-- 17_v3_vues.sql : vues des études, des avis et de la Banque de projets
-- (V_IP_FICHE et V_IP_HISTORIQUE sont redéfinies : phase courante, phase de l'historique)
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
       f.objectif_general,
       CAST(f.localisation.id_departement    AS NUMBER(6)) AS id_departement,
       CAST(f.localisation.id_arrondissement AS NUMBER(6)) AS id_arrondissement,
       CAST(f.localisation.id_commune        AS NUMBER(6)) AS id_commune,
       CAST(f.localisation.id_section        AS NUMBER(6)) AS id_section,
       f.localisation.localite                 AS localite,
       f.localisation.milieu                   AS milieu,
       (SELECT l.libelle FROM tb_ip_lieu l WHERE l.id_lieu = f.localisation.id_departement)    AS departement,
       (SELECT l.libelle FROM tb_ip_lieu l WHERE l.id_lieu = f.localisation.id_arrondissement) AS arrondissement,
       (SELECT l.libelle FROM tb_ip_lieu l WHERE l.id_lieu = f.localisation.id_commune)        AS commune,
       (SELECT l.libelle FROM tb_ip_lieu l WHERE l.id_lieu = f.localisation.id_section)        AS section_communale,
       f.donnees_probleme,
       f.donnees_qualitatives,
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
       CAST((SELECT COUNT(*) FROM tb_ip_grille g
              WHERE g.id_fiche = f.id_fiche) AS NUMBER(10)) AS nb_grilles,
       (SELECT i.intitule FROM tb_ip_idee_possible i
         WHERE i.id_fiche = f.id_fiche AND i.est_meilleure = 1) AS meilleure_idee,
       -- Phase où se trouve le projet et statut dans cette phase (pour les listes)
       NVL((SELECT MAX(e.phase) KEEP (DENSE_RANK LAST ORDER BY e.ordre_phase)
              FROM tb_ip_etude e WHERE e.id_fiche = f.id_fiche), 'IDENTIFICATION') AS phase_courante,
       NVL((SELECT MAX(e.statut) KEEP (DENSE_RANK LAST ORDER BY e.ordre_phase)
              FROM tb_ip_etude e WHERE e.id_fiche = f.id_fiche), f.statut) AS statut_phase,
       CAST((SELECT COUNT(*) FROM tb_ip_etude e
              WHERE e.id_fiche = f.id_fiche AND e.phase = 'ETUDES_PROJET'
                AND e.statut = 'APPROUVEE_DIP') AS NUMBER(1)) AS projet_valide
  FROM tb_ip_fiche f;

CREATE OR REPLACE VIEW v_ip_historique AS
SELECT CAST(h.id_historique AS NUMBER(10)) AS id_historique,
       CAST(h.id_fiche      AS NUMBER(10)) AS id_fiche,
       h.phase,
       h.action,
       h.ancien_statut,
       h.nouveau_statut,
       h.commentaire,
       h.acteur_id,
       h.acteur_nom,
       h.date_action
  FROM tb_ip_historique h;

CREATE OR REPLACE VIEW v_ip_etude AS
SELECT CAST(e.id_etude AS NUMBER(10))    AS id_etude,
       CAST(e.id_fiche AS NUMBER(10))    AS id_fiche,
       f.code_fiche,
       f.titre_projet,
       f.ministere,
       f.contact.nom_organisation         AS nom_organisation,
       f.promoteur_id,
       f.promoteur_nom,
       e.phase,
       CAST(e.ordre_phase AS NUMBER(1))  AS ordre_phase,
       e.statut,
       e.description,
       e.date_creation,
       e.date_modification,
       e.date_soumission,
       e.date_validation,
       CAST(e.est_modifiable() AS NUMBER(1)) AS est_modifiable,
       CAST(e.nb_documents()   AS NUMBER(3)) AS nb_documents
  FROM tb_ip_etude e
  JOIN tb_ip_fiche f ON f.id_fiche = e.id_fiche;

CREATE OR REPLACE VIEW v_ip_etude_document AS
SELECT CAST(d.id_document AS NUMBER(10)) AS id_document,
       CAST(d.id_etude    AS NUMBER(10)) AS id_etude,
       d.nom_fichier,
       d.type_mime,
       CAST(d.taille AS NUMBER(12))      AS taille,
       d.description,
       d.date_ajout,
       d.ajoute_par
  FROM tb_ip_etude_document d;

CREATE OR REPLACE VIEW v_ip_avis AS
SELECT CAST(a.id_avis  AS NUMBER(10)) AS id_avis,
       CAST(a.id_etude AS NUMBER(10)) AS id_etude,
       CAST(e.id_fiche AS NUMBER(10)) AS id_fiche,
       f.code_fiche,
       f.titre_projet,
       f.contact.nom_organisation      AS nom_organisation,
       e.phase,
       e.statut                        AS statut_etude,
       CAST(a.numero AS NUMBER(3))     AS numero,
       a.statut,
       a.decision,
       a.commentaire,
       a.recommandations,
       a.analyste_id, a.analyste_nom, a.date_analyste,
       a.chef_id, a.chef_nom, a.date_chef,
       a.directeur_id, a.directeur_nom, a.date_directeur,
       a.date_creation,
       a.date_modification,
       a.anomalie()                    AS anomalie
  FROM tb_ip_avis a
  JOIN tb_ip_etude e ON e.id_etude = a.id_etude
  JOIN tb_ip_fiche f ON f.id_fiche = e.id_fiche;

-- Projets validés (études de projet approuvées) : lue par le module Banque de projets
CREATE OR REPLACE VIEW v_bp_projet_valide AS
SELECT CAST(f.id_fiche AS NUMBER(10)) AS id_fiche,
       f.code_fiche,
       f.titre_projet,
       f.contact.nom_organisation      AS ministere_institution,
       f.ministere,
       f.promoteur_nom,
       (SELECT l.libelle FROM tb_ip_lieu l WHERE l.id_lieu = f.localisation.id_departement) AS departement,
       (SELECT l.libelle FROM tb_ip_lieu l WHERE l.id_lieu = f.localisation.id_commune)     AS commune,
       f.description_probleme,
       f.objectif_general,
       (SELECT MAX(h.date_action) FROM tb_ip_historique h
         WHERE h.id_fiche = f.id_fiche AND h.phase IS NULL AND h.action = 'DIP_APPROUVER') AS date_validation_fiche,
       (SELECT e.date_validation FROM tb_ip_etude e
         WHERE e.id_fiche = f.id_fiche AND e.phase = 'PREFAISABILITE') AS date_validation_prefaisabilite,
       (SELECT e.date_validation FROM tb_ip_etude e
         WHERE e.id_fiche = f.id_fiche AND e.phase = 'FAISABILITE')    AS date_validation_faisabilite,
       ep.date_validation              AS date_validation_projet
  FROM tb_ip_fiche f
  JOIN tb_ip_etude ep ON ep.id_fiche = f.id_fiche
                     AND ep.phase = 'ETUDES_PROJET'
                     AND ep.statut = 'APPROUVEE_DIP';
