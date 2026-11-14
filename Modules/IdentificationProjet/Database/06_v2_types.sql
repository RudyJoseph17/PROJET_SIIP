-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 2 : grille d'analyse du MPCE
-- 06_v2_types.sql : nouveaux types objet + évolution de T_IP_FICHE
--
-- Nouveautés de la fiche :
--   1.3  objectif général (champ distinct du problème)
--   1.4  localisation (département > arrondissement > commune > section, localité, milieu)
--   2.1  données quantitatives (attribut DONNEES_PROBLEME existant) + données qualitatives
-- =============================================================================

-- Référentiel géographique (départements, arrondissements, communes, sections)
CREATE OR REPLACE TYPE t_ip_lieu AS OBJECT (
    id_lieu   NUMBER(6),
    niveau    VARCHAR2(15),   -- DEPARTEMENT | ARRONDISSEMENT | COMMUNE | SECTION
    libelle   VARCHAR2(150),
    id_parent NUMBER(6),
    ordre     NUMBER(5)
);
/

-- 1.4 Localisation du projet (objet imbriqué dans la fiche)
CREATE OR REPLACE TYPE t_ip_localisation AS OBJECT (
    id_departement    NUMBER(6),
    id_arrondissement NUMBER(6),
    id_commune        NUMBER(6),
    id_section        NUMBER(6),   -- facultatif
    localite          VARCHAR2(200),  -- facultatif
    milieu            VARCHAR2(10),   -- URBAIN | RURAL

    MEMBER FUNCTION est_complete RETURN NUMBER
);
/

-- Questions « Généralités » de la grille (paramétrables)
CREATE OR REPLACE TYPE t_ip_question AS OBJECT (
    numero  NUMBER(3),
    libelle VARCHAR2(400),
    actif   NUMBER(1)
);
/

-- Copie d'une idée possible au moment de l'arrivée de la fiche au MPCE
CREATE OR REPLACE TYPE t_ip_piste AS OBJECT (
    ordre         NUMBER(5),
    intitule      VARCHAR2(500),
    description   CLOB,
    est_meilleure NUMBER(1)
);
/
CREATE OR REPLACE TYPE t_ip_piste_tab AS TABLE OF t_ip_piste;
/

-- Réponse à une question « Généralités » (Oui/Non + commentaire si Non)
CREATE OR REPLACE TYPE t_ip_reponse AS OBJECT (
    numero      NUMBER(3),
    question    VARCHAR2(400),
    reponse     VARCHAR2(3),      -- OUI | NON | NULL (pas encore répondu)
    commentaire VARCHAR2(2000)
);
/
CREATE OR REPLACE TYPE t_ip_reponse_tab AS TABLE OF t_ip_reponse;
/

-- Grille d'analyse de la fiche d'identification (une par arrivée au MPCE)
CREATE OR REPLACE TYPE t_ip_grille AS OBJECT (
    id_grille               NUMBER(10),
    id_fiche                NUMBER(10),
    numero                  NUMBER(3),      -- 1re, 2e… analyse de la fiche
    statut                  VARCHAR2(20),   -- EN_ANALYSE | AU_CHEF_SERVICE | AU_DIRECTEUR | APPROUVEE

    -- En-tête (rempli automatiquement)
    ministere               VARCHAR2(200),
    titre_idee              VARCHAR2(500),  -- intitulé de la meilleure idée (3.5)
    date_soumission_uep     DATE,
    date_reception_dip      DATE,

    -- Brève description : copie de la fiche telle que soumise
    departement             VARCHAR2(150),
    arrondissement          VARCHAR2(150),
    commune                 VARCHAR2(150),
    section_communale       VARCHAR2(150),
    localite                VARCHAR2(200),
    milieu                  VARCHAR2(10),
    probleme                CLOB,
    objectif_general        CLOB,
    donnees_quantitatives   CLOB,
    donnees_qualitatives    CLOB,
    pistes                  t_ip_piste_tab, -- 5. pistes de solutions
    meilleure_idee          VARCHAR2(500),  -- 6.
    legitimite_choisie      CLOB,           -- 7. informations de l'idée retenue
    pertinence_choisie      CLOB,
    viabilite_choisie       CLOB,
    commentaires_description CLOB,

    -- Analyse
    reponses                t_ip_reponse_tab,
    commentaires_generaux   CLOB,
    decision                VARCHAR2(12),   -- ACCEPTE | A_REVISER | REJETE
    recommandations         CLOB,

    -- Signatures
    analyste_id             VARCHAR2(450),
    analyste_nom            VARCHAR2(200),
    date_analyste           DATE,
    chef_id                 VARCHAR2(450),
    chef_nom                VARCHAR2(200),
    date_chef               DATE,
    directeur_id            VARCHAR2(450),
    directeur_nom           VARCHAR2(200),
    date_directeur          DATE,

    date_creation           DATE,
    date_modification       DATE,

    -- NULL si la grille peut être transmise, sinon la raison
    MEMBER FUNCTION anomalie RETURN VARCHAR2,
    MEMBER FUNCTION nb_non   RETURN NUMBER
);
/

-- Évolution de la fiche (les données existantes sont conservées)
ALTER TYPE t_ip_fiche ADD ATTRIBUTE (
    objectif_general     CLOB,
    donnees_qualitatives CLOB,
    localisation         t_ip_localisation
) CASCADE;
