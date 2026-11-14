-- =============================================================================
-- SIIP - Module "Identification projet"
-- 02_tables.sql : tables objet, contraintes, index et séquences
-- =============================================================================

CREATE SEQUENCE seq_ip_fiche      START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE seq_ip_idee       START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE seq_ip_annexe     START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE seq_ip_historique START WITH 1 INCREMENT BY 1 NOCACHE;

-- Fiches d'identification
CREATE TABLE tb_ip_fiche OF t_ip_fiche (
    CONSTRAINT pk_ip_fiche       PRIMARY KEY (id_fiche),
    CONSTRAINT uk_ip_fiche_code  UNIQUE (code_fiche),
    CONSTRAINT nn_ip_fiche_code  CHECK (code_fiche IS NOT NULL),
    CONSTRAINT nn_ip_fiche_promo CHECK (promoteur_id IS NOT NULL),
    CONSTRAINT ck_ip_fiche_statut CHECK (statut IN (
        'BROUILLON',       -- en cours de rédaction par le promoteur
        'SOUMISE_UEP',     -- soumise à l'Unité d'Études et de Programmation
        'COMPLEMENT_UEP',  -- l'UEP demande des informations supplémentaires
        'REJETEE_UEP',
        'TRANSMISE_DIP',   -- approuvée par l'UEP, transmise à la DIP
        'COMPLEMENT_DIP',  -- la DIP demande des informations supplémentaires
        'REJETEE_DIP',
        'APPROUVEE_DIP'))  -- autorisée à passer à l'élaboration de l'Avant-Projet
)
OBJECT IDENTIFIER IS PRIMARY KEY;

CREATE INDEX ix_ip_fiche_promoteur ON tb_ip_fiche (promoteur_id);
CREATE INDEX ix_ip_fiche_statut    ON tb_ip_fiche (statut);

-- Idées possibles (nombre illimité par fiche)
CREATE TABLE tb_ip_idee_possible OF t_ip_idee_possible (
    CONSTRAINT pk_ip_idee        PRIMARY KEY (id_idee),
    CONSTRAINT fk_ip_idee_fiche  FOREIGN KEY (id_fiche)
        REFERENCES tb_ip_fiche (id_fiche) ON DELETE CASCADE,
    CONSTRAINT nn_ip_idee_fiche  CHECK (id_fiche IS NOT NULL),
    CONSTRAINT ck_ip_idee_best   CHECK (est_meilleure IN (0, 1))
)
OBJECT IDENTIFIER IS PRIMARY KEY;

CREATE INDEX ix_ip_idee_fiche ON tb_ip_idee_possible (id_fiche, ordre);

-- Au plus une "meilleure idée" par fiche
CREATE UNIQUE INDEX ux_ip_idee_meilleure ON tb_ip_idee_possible (
    CASE WHEN est_meilleure = 1 THEN id_fiche END);

-- Annexes (le maximum de 3 est contrôlé par le package)
CREATE TABLE tb_ip_annexe OF t_ip_annexe (
    CONSTRAINT pk_ip_annexe       PRIMARY KEY (id_annexe),
    CONSTRAINT fk_ip_annexe_fiche FOREIGN KEY (id_fiche)
        REFERENCES tb_ip_fiche (id_fiche) ON DELETE CASCADE,
    CONSTRAINT nn_ip_annexe_fiche CHECK (id_fiche IS NOT NULL),
    CONSTRAINT nn_ip_annexe_nom   CHECK (nom_fichier IS NOT NULL)
)
OBJECT IDENTIFIER IS PRIMARY KEY
LOB (contenu) STORE AS SECUREFILE (ENABLE STORAGE IN ROW);

CREATE INDEX ix_ip_annexe_fiche ON tb_ip_annexe (id_fiche);

-- Historique
CREATE TABLE tb_ip_historique OF t_ip_historique (
    CONSTRAINT pk_ip_historique       PRIMARY KEY (id_historique),
    CONSTRAINT fk_ip_historique_fiche FOREIGN KEY (id_fiche)
        REFERENCES tb_ip_fiche (id_fiche) ON DELETE CASCADE
)
OBJECT IDENTIFIER IS PRIMARY KEY;

CREATE INDEX ix_ip_historique_fiche ON tb_ip_historique (id_fiche, date_action);
