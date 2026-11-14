-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 3 : phases d'études
-- 14_v3_types.sql : types objet des études et des avis du MPCE
--
-- Après validation de la fiche d'identification, le projet passe par trois phases,
-- chacune avec ses pièces jointes et le même circuit que la fiche
-- (promoteur -> UEP -> MPCE : Analyste -> Chef de Service -> Directeur) :
--   1. PREFAISABILITE   2. FAISABILITE   3. ETUDES_PROJET (validation finale)
-- =============================================================================

-- L'historique indique désormais la phase concernée (NULL = identification)
ALTER TYPE t_ip_historique ADD ATTRIBUTE (phase VARCHAR2(20)) CASCADE;

-- Étude d'une phase (une par projet et par phase)
CREATE OR REPLACE TYPE t_ip_etude AS OBJECT (
    id_etude          NUMBER(10),
    id_fiche          NUMBER(10),
    phase             VARCHAR2(20),   -- PREFAISABILITE | FAISABILITE | ETUDES_PROJET
    ordre_phase       NUMBER(1),      -- 1 | 2 | 3
    statut            VARCHAR2(20),   -- mêmes statuts que la fiche
    description       CLOB,           -- note de présentation du promoteur
    date_creation     DATE,
    date_modification DATE,
    date_soumission   DATE,
    date_validation   DATE,

    MEMBER FUNCTION est_modifiable RETURN NUMBER,
    MEMBER FUNCTION nb_documents   RETURN NUMBER
);
/

-- Pièce jointe d'une étude
CREATE OR REPLACE TYPE t_ip_etude_document AS OBJECT (
    id_document NUMBER(10),
    id_etude    NUMBER(10),
    nom_fichier VARCHAR2(255),
    type_mime   VARCHAR2(100),
    taille      NUMBER(12),
    contenu     BLOB,
    description VARCHAR2(300),
    date_ajout  DATE,
    ajoute_par  VARCHAR2(200)
);
/

-- Avis du MPCE sur une étude (un par arrivée de l'étude au MPCE)
CREATE OR REPLACE TYPE t_ip_avis AS OBJECT (
    id_avis           NUMBER(10),
    id_etude          NUMBER(10),
    numero            NUMBER(3),
    statut            VARCHAR2(20),   -- EN_ANALYSE | AU_CHEF_SERVICE | AU_DIRECTEUR | APPROUVEE
    decision          VARCHAR2(12),   -- ACCEPTE | A_REVISER | REJETE
    commentaire       CLOB,           -- avis motivé de l'analyste
    recommandations   CLOB,           -- transmises au promoteur
    analyste_id       VARCHAR2(450),
    analyste_nom      VARCHAR2(200),
    date_analyste     DATE,
    chef_id           VARCHAR2(450),
    chef_nom          VARCHAR2(200),
    date_chef         DATE,
    directeur_id      VARCHAR2(450),
    directeur_nom     VARCHAR2(200),
    date_directeur    DATE,
    date_creation     DATE,
    date_modification DATE,

    MEMBER FUNCTION anomalie RETURN VARCHAR2
);
/
