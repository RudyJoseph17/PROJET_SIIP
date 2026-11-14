-- =============================================================================
-- SIIP - Module "Identification projet"
-- 01_types.sql : types objet Oracle (spécifications)
--
-- Fiche d'Identification de l'Idée de Projet (Manuel de procédures, Tome I,
-- Annexe 3). Ordre d'exécution : 01_types -> 02_tables -> 03_types_corps
--                                -> 04_vues -> 05_package
-- =============================================================================

-- 1.1 Contact principal (objet imbriqué dans la fiche)
CREATE OR REPLACE TYPE t_ip_contact AS OBJECT (
    nom_organisation VARCHAR2(200),
    nom_contact      VARCHAR2(150),
    telephone        VARCHAR2(30),
    courriel         VARCHAR2(150),

    -- 1 si les informations minimales du contact sont renseignées
    MEMBER FUNCTION est_complet RETURN NUMBER
);
/

-- Fiche d'identification (sections 1, 2, 3.5, 4 + suivi)
CREATE OR REPLACE TYPE t_ip_fiche AS OBJECT (
    id_fiche             NUMBER(10),
    code_fiche           VARCHAR2(20),
    contact              t_ip_contact,   -- 1.1
    titre_projet         VARCHAR2(500),  -- 1.2 (saisi après le choix de la meilleure idée)
    description_probleme CLOB,           -- 1.3
    donnees_probleme     CLOB,           -- 2.1
    consequences         CLOB,           -- 2.2
    justification_choix  CLOB,           -- 3.5
    conclusions          CLOB,           -- 4.1
    statut               VARCHAR2(20),
    promoteur_id         VARCHAR2(450),  -- AspNetUsers.Id
    promoteur_nom        VARCHAR2(200),
    ministere            VARCHAR2(200),  -- ministère du promoteur (UEP compétente)
    date_creation        DATE,
    date_modification    DATE,
    date_soumission      DATE,

    -- p_etape : 1 Introduction, 2 Importance, 3 Idées possibles,
    --           4 Meilleure idée + titre, 5 Conclusion
    MEMBER FUNCTION etape_complete (p_etape IN NUMBER) RETURN NUMBER,
    MEMBER FUNCTION progression     RETURN NUMBER,   -- 0..100
    MEMBER FUNCTION est_modifiable  RETURN NUMBER    -- 1 si le promoteur peut modifier
) NOT FINAL;
/

-- 3.1 à 3.4 : une idée possible avec sa légitimité, pertinence et viabilité
CREATE OR REPLACE TYPE t_ip_idee_possible AS OBJECT (
    id_idee       NUMBER(10),
    id_fiche      NUMBER(10),
    ordre         NUMBER(5),
    intitule      VARCHAR2(500),  -- 3.1
    description   CLOB,           -- 3.1 (brève description)
    legitimite    CLOB,           -- 3.2
    pertinence    CLOB,           -- 3.3
    viabilite     CLOB,           -- 3.4
    est_meilleure NUMBER(1),      -- 3.5 (une seule par fiche)

    MEMBER FUNCTION est_complete RETURN NUMBER
);
/

-- Section 5 : documents annexes (3 au maximum par fiche)
CREATE OR REPLACE TYPE t_ip_annexe AS OBJECT (
    id_annexe   NUMBER(10),
    id_fiche    NUMBER(10),
    nom_fichier VARCHAR2(255),
    type_mime   VARCHAR2(100),
    taille      NUMBER(12),
    contenu     BLOB,
    description VARCHAR2(300),
    date_ajout  DATE,
    ajoute_par  VARCHAR2(200)
);
/

-- Historique des changements de statut (suivi de la progression)
CREATE OR REPLACE TYPE t_ip_historique AS OBJECT (
    id_historique  NUMBER(10),
    id_fiche       NUMBER(10),
    action         VARCHAR2(30),
    ancien_statut  VARCHAR2(20),
    nouveau_statut VARCHAR2(20),
    commentaire    VARCHAR2(4000),
    acteur_id      VARCHAR2(450),
    acteur_nom     VARCHAR2(200),
    date_action    DATE
);
/
