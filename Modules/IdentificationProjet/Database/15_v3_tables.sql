-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 3
-- 15_v3_tables.sql : tables objet des études, pièces jointes et avis
-- =============================================================================

CREATE SEQUENCE seq_ip_etude          START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE seq_ip_etude_document START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE seq_ip_avis           START WITH 1 INCREMENT BY 1 NOCACHE;

CREATE TABLE tb_ip_etude OF t_ip_etude (
    CONSTRAINT pk_ip_etude        PRIMARY KEY (id_etude),
    CONSTRAINT fk_ip_etude_fiche  FOREIGN KEY (id_fiche)
        REFERENCES tb_ip_fiche (id_fiche) ON DELETE CASCADE,
    CONSTRAINT uk_ip_etude_phase  UNIQUE (id_fiche, phase),
    CONSTRAINT ck_ip_etude_phase  CHECK (phase IN ('PREFAISABILITE', 'FAISABILITE', 'ETUDES_PROJET')),
    CONSTRAINT ck_ip_etude_ordre  CHECK (ordre_phase IN (1, 2, 3)),
    CONSTRAINT ck_ip_etude_statut CHECK (statut IN (
        'BROUILLON', 'SOUMISE_UEP', 'COMPLEMENT_UEP', 'REJETEE_UEP',
        'TRANSMISE_DIP', 'COMPLEMENT_DIP', 'REJETEE_DIP', 'APPROUVEE_DIP'))
)
OBJECT IDENTIFIER IS PRIMARY KEY;

CREATE INDEX ix_ip_etude_statut ON tb_ip_etude (statut);

CREATE TABLE tb_ip_etude_document OF t_ip_etude_document (
    CONSTRAINT pk_ip_etude_doc       PRIMARY KEY (id_document),
    CONSTRAINT fk_ip_etude_doc_etude FOREIGN KEY (id_etude)
        REFERENCES tb_ip_etude (id_etude) ON DELETE CASCADE,
    CONSTRAINT nn_ip_etude_doc_nom   CHECK (nom_fichier IS NOT NULL)
)
OBJECT IDENTIFIER IS PRIMARY KEY
LOB (contenu) STORE AS SECUREFILE (ENABLE STORAGE IN ROW);

CREATE INDEX ix_ip_etude_doc_etude ON tb_ip_etude_document (id_etude);

CREATE TABLE tb_ip_avis OF t_ip_avis (
    CONSTRAINT pk_ip_avis          PRIMARY KEY (id_avis),
    CONSTRAINT fk_ip_avis_etude    FOREIGN KEY (id_etude)
        REFERENCES tb_ip_etude (id_etude) ON DELETE CASCADE,
    CONSTRAINT uk_ip_avis_numero   UNIQUE (id_etude, numero),
    CONSTRAINT ck_ip_avis_statut   CHECK (statut IN ('EN_ANALYSE', 'AU_CHEF_SERVICE', 'AU_DIRECTEUR', 'APPROUVEE')),
    CONSTRAINT ck_ip_avis_decision CHECK (decision IS NULL OR decision IN ('ACCEPTE', 'A_REVISER', 'REJETE'))
)
OBJECT IDENTIFIER IS PRIMARY KEY;

CREATE INDEX ix_ip_avis_statut ON tb_ip_avis (statut);

-- Au plus un avis en cours (non approuvé) par étude
CREATE UNIQUE INDEX ux_ip_avis_en_cours ON tb_ip_avis (
    CASE WHEN statut <> 'APPROUVEE' THEN id_etude END);
