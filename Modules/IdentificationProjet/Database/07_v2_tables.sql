-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 2
-- 07_v2_tables.sql : tables objet du référentiel et des grilles d'analyse
-- =============================================================================

CREATE SEQUENCE seq_ip_grille START WITH 1 INCREMENT BY 1 NOCACHE;

-- Référentiel géographique
CREATE TABLE tb_ip_lieu OF t_ip_lieu (
    CONSTRAINT pk_ip_lieu        PRIMARY KEY (id_lieu),
    CONSTRAINT fk_ip_lieu_parent FOREIGN KEY (id_parent) REFERENCES tb_ip_lieu (id_lieu),
    CONSTRAINT ck_ip_lieu_niveau CHECK (niveau IN ('DEPARTEMENT', 'ARRONDISSEMENT', 'COMMUNE', 'SECTION')),
    CONSTRAINT nn_ip_lieu_lib    CHECK (libelle IS NOT NULL)
)
OBJECT IDENTIFIER IS PRIMARY KEY;

CREATE INDEX ix_ip_lieu_parent ON tb_ip_lieu (id_parent, ordre);

-- Contrôle de la localisation de la fiche (objet imbriqué)
ALTER TABLE tb_ip_fiche ADD CONSTRAINT ck_ip_fiche_milieu
    CHECK (localisation.milieu IS NULL OR localisation.milieu IN ('URBAIN', 'RURAL'));

-- Questions « Généralités »
CREATE TABLE tb_ip_question OF t_ip_question (
    CONSTRAINT pk_ip_question       PRIMARY KEY (numero),
    CONSTRAINT nn_ip_question_lib   CHECK (libelle IS NOT NULL),
    CONSTRAINT ck_ip_question_actif CHECK (actif IN (0, 1))
)
OBJECT IDENTIFIER IS PRIMARY KEY;

-- Grilles d'analyse : les idées copiées et les réponses sont des tables imbriquées
CREATE TABLE tb_ip_grille OF t_ip_grille (
    CONSTRAINT pk_ip_grille        PRIMARY KEY (id_grille),
    CONSTRAINT fk_ip_grille_fiche  FOREIGN KEY (id_fiche)
        REFERENCES tb_ip_fiche (id_fiche) ON DELETE CASCADE,
    CONSTRAINT uk_ip_grille_numero UNIQUE (id_fiche, numero),
    CONSTRAINT ck_ip_grille_statut CHECK (statut IN ('EN_ANALYSE', 'AU_CHEF_SERVICE', 'AU_DIRECTEUR', 'APPROUVEE')),
    CONSTRAINT ck_ip_grille_decision CHECK (decision IS NULL OR decision IN ('ACCEPTE', 'A_REVISER', 'REJETE'))
)
OBJECT IDENTIFIER IS PRIMARY KEY
NESTED TABLE pistes   STORE AS tb_ip_grille_pistes
NESTED TABLE reponses STORE AS tb_ip_grille_reponses;

CREATE INDEX ix_ip_grille_statut ON tb_ip_grille (statut);

-- Au plus une grille en cours (non approuvée) par fiche
CREATE UNIQUE INDEX ux_ip_grille_en_cours ON tb_ip_grille (
    CASE WHEN statut <> 'APPROUVEE' THEN id_fiche END);
