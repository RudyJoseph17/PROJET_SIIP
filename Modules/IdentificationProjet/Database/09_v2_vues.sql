-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 2
-- 09_v2_vues.sql : vues relationnelles (remplace V_IP_FICHE, ajoute les grilles)
-- =============================================================================

CREATE OR REPLACE VIEW v_ip_lieu AS
SELECT CAST(l.id_lieu AS NUMBER(6))   AS id_lieu,
       l.niveau,
       l.libelle,
       CAST(l.id_parent AS NUMBER(6)) AS id_parent,
       CAST(l.ordre AS NUMBER(5))     AS ordre
  FROM tb_ip_lieu l;

CREATE OR REPLACE VIEW v_ip_question AS
SELECT CAST(q.numero AS NUMBER(3)) AS numero, q.libelle, CAST(q.actif AS NUMBER(1)) AS actif
  FROM tb_ip_question q;

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
         WHERE i.id_fiche = f.id_fiche AND i.est_meilleure = 1) AS meilleure_idee
  FROM tb_ip_fiche f;

CREATE OR REPLACE VIEW v_ip_grille AS
SELECT CAST(g.id_grille AS NUMBER(10)) AS id_grille,
       CAST(g.id_fiche  AS NUMBER(10)) AS id_fiche,
       CAST(g.numero    AS NUMBER(3))  AS numero,
       f.code_fiche,
       f.statut                        AS statut_fiche,
       f.promoteur_nom,
       g.statut,
       g.ministere, g.titre_idee, g.date_soumission_uep, g.date_reception_dip,
       g.departement, g.arrondissement, g.commune, g.section_communale, g.localite, g.milieu,
       g.probleme, g.objectif_general, g.donnees_quantitatives, g.donnees_qualitatives,
       g.meilleure_idee, g.legitimite_choisie, g.pertinence_choisie, g.viabilite_choisie,
       g.commentaires_description, g.commentaires_generaux, g.decision, g.recommandations,
       g.analyste_id, g.analyste_nom, g.date_analyste,
       g.chef_id, g.chef_nom, g.date_chef,
       g.directeur_id, g.directeur_nom, g.date_directeur,
       g.date_creation, g.date_modification,
       g.anomalie()                    AS anomalie,
       CAST(g.nb_non() AS NUMBER(3))   AS nb_non
  FROM tb_ip_grille g
  JOIN tb_ip_fiche f ON f.id_fiche = g.id_fiche;

-- Tables imbriquées "dépliées" pour EF Core
CREATE OR REPLACE VIEW v_ip_grille_piste AS
SELECT CAST(g.id_grille AS NUMBER(10)) AS id_grille,
       CAST(p.ordre AS NUMBER(5))      AS ordre,
       p.intitule,
       p.description,
       CAST(p.est_meilleure AS NUMBER(1)) AS est_meilleure
  FROM tb_ip_grille g, TABLE(g.pistes) p;

CREATE OR REPLACE VIEW v_ip_grille_reponse AS
SELECT CAST(g.id_grille AS NUMBER(10)) AS id_grille,
       CAST(r.numero AS NUMBER(3))     AS numero,
       r.question,
       r.reponse,
       r.commentaire
  FROM tb_ip_grille g, TABLE(g.reponses) r;
