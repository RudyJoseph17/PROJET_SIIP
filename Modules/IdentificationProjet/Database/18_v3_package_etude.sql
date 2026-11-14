-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 3
-- 18_v3_package_etude.sql : API PL/SQL des phases d'études
--
-- Enchaînement : fiche validée -> PREFAISABILITE -> FAISABILITE -> ETUDES_PROJET
-- (chaque phase s'ouvre automatiquement, en brouillon, à la validation de la précédente ;
--  la validation des ETUDES_PROJET fait du projet un « projet validé » : Banque de projets)
--
-- Circuit d'une étude (mêmes statuts que la fiche) :
--   BROUILLON --SOUMETTRE--> SOUMISE_UEP --UEP_APPROUVER--> TRANSMISE_DIP (au MPCE)
--   SOUMISE_UEP --UEP_COMPLEMENT--> COMPLEMENT_UEP --SOUMETTRE--> SOUMISE_UEP
--   SOUMISE_UEP --UEP_REJETER--> REJETEE_UEP
--   TRANSMISE_DIP : un AVIS est généré (Analyste -> Chef de Service -> Directeur) ;
--   l'approbation de l'avis applique sa décision :
--     ACCEPTE -> APPROUVEE_DIP (phase suivante)  A_REVISER -> COMPLEMENT_DIP  REJETE -> REJETEE_DIP
--   COMPLEMENT_DIP --SOUMETTRE--> TRANSMISE_DIP (nouvel avis)
--
-- Erreurs : mêmes codes que PKG_IP_IDENTIFICATION ; 20015 = aucune pièce jointe.
-- =============================================================================

CREATE OR REPLACE PACKAGE pkg_ip_etude AS

    c_max_documents   CONSTANT PLS_INTEGER := 10;
    c_taille_max_octs CONSTANT PLS_INTEGER := 20 * 1024 * 1024;  -- 20 Mo

    -- Ouvre (en brouillon) la phase d'un projet si elle n'existe pas encore
    PROCEDURE ouvrir_phase (p_id_fiche IN NUMBER, p_phase IN VARCHAR2);

    PROCEDURE maj_etude (p_id_etude IN NUMBER, p_description IN CLOB);

    PROCEDURE ajouter_document (
        p_id_etude    IN  NUMBER,
        p_nom_fichier IN  VARCHAR2,
        p_type_mime   IN  VARCHAR2,
        p_contenu     IN  BLOB,
        p_description IN  VARCHAR2,
        p_ajoute_par  IN  VARCHAR2,
        p_id_document OUT NUMBER);

    PROCEDURE supprimer_document (p_id_etude IN NUMBER, p_id_document IN NUMBER);

    -- Actions du promoteur et de l'UEP ; la décision du MPCE passe par l'avis
    PROCEDURE changer_statut (
        p_id_etude    IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2);

    -- Avis du MPCE
    PROCEDURE maj_avis (
        p_id_avis         IN NUMBER,
        p_decision        IN VARCHAR2,
        p_commentaire     IN CLOB,
        p_recommandations IN CLOB);

    PROCEDURE action_avis (
        p_id_avis     IN NUMBER,
        p_action      IN VARCHAR2,   -- ANALYSTE_TRANSMETTRE | CHEF_VALIDER | CHEF_RENVOYER
                                     -- | DIRECTEUR_APPROUVER | DIRECTEUR_RENVOYER
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2);

    FUNCTION phase_suivante (p_phase IN VARCHAR2) RETURN VARCHAR2;

END pkg_ip_etude;
/

CREATE OR REPLACE PACKAGE BODY pkg_ip_etude AS

    -- =========================================================================
    -- Utilitaires
    -- =========================================================================
    FUNCTION ordre_de (p_phase IN VARCHAR2) RETURN NUMBER IS
    BEGIN
        RETURN CASE p_phase WHEN 'PREFAISABILITE' THEN 1 WHEN 'FAISABILITE' THEN 2
                            WHEN 'ETUDES_PROJET' THEN 3 END;
    END ordre_de;

    FUNCTION phase_suivante (p_phase IN VARCHAR2) RETURN VARCHAR2 IS
    BEGIN
        RETURN CASE p_phase WHEN 'PREFAISABILITE' THEN 'FAISABILITE'
                            WHEN 'FAISABILITE'    THEN 'ETUDES_PROJET' END;  -- fin du cycle après ETUDES_PROJET
    END phase_suivante;

    FUNCTION charger (p_id_etude IN NUMBER) RETURN t_ip_etude IS
        v t_ip_etude;
    BEGIN
        SELECT VALUE(e) INTO v FROM tb_ip_etude e WHERE e.id_etude = p_id_etude FOR UPDATE;
        RETURN v;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20001, 'Étude introuvable.');
    END charger;

    FUNCTION charger_avis (p_id_avis IN NUMBER) RETURN t_ip_avis IS
        v t_ip_avis;
    BEGIN
        SELECT VALUE(a) INTO v FROM tb_ip_avis a WHERE a.id_avis = p_id_avis FOR UPDATE;
        RETURN v;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20013, 'Avis introuvable.');
    END charger_avis;

    PROCEDURE verifier_modifiable (p_id_etude IN NUMBER) IS
        v t_ip_etude := charger(p_id_etude);
    BEGIN
        IF v.est_modifiable() = 0 THEN
            RAISE_APPLICATION_ERROR(-20002, 'L''étude n''est pas modifiable dans son statut actuel.');
        END IF;
    END verifier_modifiable;

    PROCEDURE toucher (p_id_etude IN NUMBER) IS
    BEGIN
        UPDATE tb_ip_etude e SET e.date_modification = SYSDATE WHERE e.id_etude = p_id_etude;
    END toucher;

    PROCEDURE journaliser (
        p_id_fiche IN NUMBER, p_phase IN VARCHAR2, p_action IN VARCHAR2,
        p_ancien IN VARCHAR2, p_nouveau IN VARCHAR2, p_commentaire IN VARCHAR2,
        p_acteur_id IN VARCHAR2, p_acteur_nom IN VARCHAR2)
    IS
    BEGIN
        INSERT INTO tb_ip_historique
        VALUES (t_ip_historique(seq_ip_historique.NEXTVAL, p_id_fiche, p_action,
                                p_ancien, p_nouveau, SUBSTR(p_commentaire, 1, 4000),
                                p_acteur_id, p_acteur_nom, SYSDATE, p_phase));
    END journaliser;

    -- =========================================================================
    -- Étude
    -- =========================================================================
    PROCEDURE ouvrir_phase (p_id_fiche IN NUMBER, p_phase IN VARCHAR2) IS
        v_nb    PLS_INTEGER;
        v_ordre NUMBER := ordre_de(p_phase);   -- fonction privée : hors de l'ordre SQL
    BEGIN
        IF v_ordre IS NULL THEN
            RAISE_APPLICATION_ERROR(-20009, 'Phase inconnue : ' || p_phase);
        END IF;
        SELECT COUNT(*) INTO v_nb FROM tb_ip_etude e
         WHERE e.id_fiche = p_id_fiche AND e.phase = p_phase;
        IF v_nb > 0 THEN
            RETURN;
        END IF;
        INSERT INTO tb_ip_etude
        VALUES (t_ip_etude(seq_ip_etude.NEXTVAL, p_id_fiche, p_phase, v_ordre,
                           'BROUILLON', NULL, SYSDATE, SYSDATE, NULL, NULL));
        journaliser(p_id_fiche, p_phase, 'OUVERTURE', NULL, 'BROUILLON', NULL, NULL, 'Système');
    END ouvrir_phase;

    PROCEDURE maj_etude (p_id_etude IN NUMBER, p_description IN CLOB) IS
    BEGIN
        verifier_modifiable(p_id_etude);
        UPDATE tb_ip_etude e
           SET e.description = p_description, e.date_modification = SYSDATE
         WHERE e.id_etude = p_id_etude;
    END maj_etude;

    PROCEDURE ajouter_document (
        p_id_etude    IN  NUMBER,
        p_nom_fichier IN  VARCHAR2,
        p_type_mime   IN  VARCHAR2,
        p_contenu     IN  BLOB,
        p_description IN  VARCHAR2,
        p_ajoute_par  IN  VARCHAR2,
        p_id_document OUT NUMBER)
    IS
        v_nb     PLS_INTEGER;
        v_taille NUMBER := NVL(DBMS_LOB.GETLENGTH(p_contenu), 0);
    BEGIN
        verifier_modifiable(p_id_etude);   -- verrouille l'étude
        SELECT COUNT(*) INTO v_nb FROM tb_ip_etude_document d WHERE d.id_etude = p_id_etude;
        IF v_nb >= c_max_documents THEN
            RAISE_APPLICATION_ERROR(-20006,
                'Une étude ne peut pas avoir plus de ' || c_max_documents || ' pièces jointes.');
        END IF;
        IF v_taille = 0 THEN
            RAISE_APPLICATION_ERROR(-20010, 'Le fichier est vide.');
        END IF;
        IF v_taille > c_taille_max_octs THEN
            RAISE_APPLICATION_ERROR(-20010, 'Le fichier dépasse la taille maximale de 20 Mo.');
        END IF;

        p_id_document := seq_ip_etude_document.NEXTVAL;
        INSERT INTO tb_ip_etude_document
        VALUES (t_ip_etude_document(p_id_document, p_id_etude, p_nom_fichier, p_type_mime,
                                    v_taille, p_contenu, p_description, SYSDATE, p_ajoute_par));
        toucher(p_id_etude);
    END ajouter_document;

    PROCEDURE supprimer_document (p_id_etude IN NUMBER, p_id_document IN NUMBER) IS
    BEGIN
        verifier_modifiable(p_id_etude);
        DELETE FROM tb_ip_etude_document d
         WHERE d.id_document = p_id_document AND d.id_etude = p_id_etude;
        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(-20007, 'Pièce jointe introuvable pour cette étude.');
        END IF;
        toucher(p_id_etude);
    END supprimer_document;

    -- =========================================================================
    -- Avis du MPCE : génération à chaque arrivée de l'étude au MPCE
    -- =========================================================================
    PROCEDURE generer_avis (p_id_etude IN NUMBER, p_id_fiche IN NUMBER, p_phase IN VARCHAR2) IS
        v_nb     PLS_INTEGER;
        v_numero NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_nb FROM tb_ip_avis a
         WHERE a.id_etude = p_id_etude AND a.statut <> 'APPROUVEE';
        IF v_nb > 0 THEN
            RETURN;
        END IF;
        SELECT NVL(MAX(a.numero), 0) + 1 INTO v_numero FROM tb_ip_avis a WHERE a.id_etude = p_id_etude;
        INSERT INTO tb_ip_avis
        VALUES (t_ip_avis(seq_ip_avis.NEXTVAL, p_id_etude, v_numero, 'EN_ANALYSE',
                          NULL, NULL, NULL,
                          NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                          SYSDATE, SYSDATE));
        journaliser(p_id_fiche, p_phase, 'AVIS_GENERE', NULL, 'EN_ANALYSE',
                    'Avis n° ' || v_numero || ' à rédiger.', NULL, 'Système');
    END generer_avis;

    -- =========================================================================
    -- Transitions de l'étude
    -- =========================================================================
    PROCEDURE appliquer_transition (
        p_id_etude    IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2)
    IS
        v         t_ip_etude := charger(p_id_etude);
        v_attendu VARCHAR2(20);
        v_nouveau VARCHAR2(20);

        PROCEDURE exiger_commentaire IS
        BEGIN
            IF TRIM(p_commentaire) IS NULL THEN
                RAISE_APPLICATION_ERROR(-20005,
                    'Un commentaire est obligatoire pour demander une correction ou rejeter l''étude.');
            END IF;
        END;
    BEGIN
        CASE p_action
            WHEN 'SOUMETTRE' THEN
                IF v.est_modifiable() = 0 THEN
                    RAISE_APPLICATION_ERROR(-20004, 'L''étude ne peut pas être soumise dans son statut actuel.');
                END IF;
                IF v.nb_documents() = 0 THEN
                    RAISE_APPLICATION_ERROR(-20015, 'Joignez au moins un document avant de soumettre l''étude.');
                END IF;
                v_attendu := v.statut;
                v_nouveau := CASE v.statut WHEN 'COMPLEMENT_DIP' THEN 'TRANSMISE_DIP' ELSE 'SOUMISE_UEP' END;
            WHEN 'UEP_APPROUVER'  THEN v_attendu := 'SOUMISE_UEP';   v_nouveau := 'TRANSMISE_DIP';
            WHEN 'UEP_COMPLEMENT' THEN v_attendu := 'SOUMISE_UEP';   v_nouveau := 'COMPLEMENT_UEP'; exiger_commentaire;
            WHEN 'UEP_REJETER'    THEN v_attendu := 'SOUMISE_UEP';   v_nouveau := 'REJETEE_UEP';    exiger_commentaire;
            WHEN 'DIP_APPROUVER'  THEN v_attendu := 'TRANSMISE_DIP'; v_nouveau := 'APPROUVEE_DIP';
            WHEN 'DIP_COMPLEMENT' THEN v_attendu := 'TRANSMISE_DIP'; v_nouveau := 'COMPLEMENT_DIP'; exiger_commentaire;
            WHEN 'DIP_REJETER'    THEN v_attendu := 'TRANSMISE_DIP'; v_nouveau := 'REJETEE_DIP';    exiger_commentaire;
            ELSE
                RAISE_APPLICATION_ERROR(-20009, 'Action inconnue : ' || p_action);
        END CASE;

        IF v.statut <> v_attendu THEN
            RAISE_APPLICATION_ERROR(-20004, 'Action impossible : l''étude est au statut ' || v.statut || '.');
        END IF;

        UPDATE tb_ip_etude e
           SET e.statut            = v_nouveau,
               e.date_soumission   = CASE WHEN p_action = 'SOUMETTRE' THEN SYSDATE ELSE e.date_soumission END,
               e.date_validation   = CASE WHEN v_nouveau = 'APPROUVEE_DIP' THEN SYSDATE ELSE e.date_validation END,
               e.date_modification = SYSDATE
         WHERE e.id_etude = p_id_etude;

        journaliser(v.id_fiche, v.phase, p_action, v.statut, v_nouveau, p_commentaire,
                    p_acteur_id, p_acteur_nom);

        IF v_nouveau = 'TRANSMISE_DIP' THEN
            generer_avis(p_id_etude, v.id_fiche, v.phase);
        ELSIF v_nouveau = 'APPROUVEE_DIP' THEN
            IF phase_suivante(v.phase) IS NOT NULL THEN
                ouvrir_phase(v.id_fiche, phase_suivante(v.phase));
            ELSE
                journaliser(v.id_fiche, v.phase, 'PROJET_VALIDE', NULL, NULL,
                            'Projet validé : inscrit à la Banque de projets.', p_acteur_id, p_acteur_nom);
            END IF;
        END IF;
    END appliquer_transition;

    PROCEDURE changer_statut (
        p_id_etude    IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2)
    IS
    BEGIN
        IF p_action LIKE 'DIP\_%' ESCAPE '\' THEN
            RAISE_APPLICATION_ERROR(-20004,
                'La décision du MPCE est prise par l''approbation de l''avis.');
        END IF;
        appliquer_transition(p_id_etude, p_action, p_commentaire, p_acteur_id, p_acteur_nom);
    END changer_statut;

    -- =========================================================================
    -- Avis : saisie et circuit Analyste -> Chef de Service -> Directeur
    -- =========================================================================
    PROCEDURE maj_avis (
        p_id_avis         IN NUMBER,
        p_decision        IN VARCHAR2,
        p_commentaire     IN CLOB,
        p_recommandations IN CLOB)
    IS
        v t_ip_avis := charger_avis(p_id_avis);
    BEGIN
        IF v.statut <> 'EN_ANALYSE' THEN
            RAISE_APPLICATION_ERROR(-20013, 'L''avis n''est plus modifiable : il a été transmis pour validation.');
        END IF;
        UPDATE tb_ip_avis a
           SET a.decision = p_decision, a.commentaire = p_commentaire,
               a.recommandations = p_recommandations, a.date_modification = SYSDATE
         WHERE a.id_avis = p_id_avis;
    END maj_avis;

    PROCEDURE action_avis (
        p_id_avis     IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2)
    IS
        v         t_ip_avis := charger_avis(p_id_avis);
        v_etude   t_ip_etude;
        v_attendu VARCHAR2(20);
        v_nouveau VARCHAR2(20);
        v_anomalie VARCHAR2(400);

        PROCEDURE exiger_commentaire IS
        BEGIN
            IF TRIM(p_commentaire) IS NULL THEN
                RAISE_APPLICATION_ERROR(-20005, 'Un commentaire est obligatoire pour renvoyer l''avis à l''analyste.');
            END IF;
        END;
    BEGIN
        CASE p_action
            WHEN 'ANALYSTE_TRANSMETTRE' THEN v_attendu := 'EN_ANALYSE';      v_nouveau := 'AU_CHEF_SERVICE';
            WHEN 'CHEF_VALIDER'         THEN v_attendu := 'AU_CHEF_SERVICE'; v_nouveau := 'AU_DIRECTEUR';
            WHEN 'CHEF_RENVOYER'        THEN v_attendu := 'AU_CHEF_SERVICE'; v_nouveau := 'EN_ANALYSE'; exiger_commentaire;
            WHEN 'DIRECTEUR_APPROUVER'  THEN v_attendu := 'AU_DIRECTEUR';    v_nouveau := 'APPROUVEE';
            WHEN 'DIRECTEUR_RENVOYER'   THEN v_attendu := 'AU_DIRECTEUR';    v_nouveau := 'EN_ANALYSE'; exiger_commentaire;
            ELSE
                RAISE_APPLICATION_ERROR(-20009, 'Action inconnue : ' || p_action);
        END CASE;

        IF v.statut <> v_attendu THEN
            RAISE_APPLICATION_ERROR(-20004, 'Action impossible : l''avis est au statut ' || v.statut || '.');
        END IF;
        IF p_action = 'ANALYSTE_TRANSMETTRE' THEN
            v_anomalie := v.anomalie();
            IF v_anomalie IS NOT NULL THEN
                RAISE_APPLICATION_ERROR(-20011, 'Avis incomplet : ' || v_anomalie);
            END IF;
        END IF;

        UPDATE tb_ip_avis a
           SET a.statut        = v_nouveau,
               a.analyste_id   = CASE WHEN p_action = 'ANALYSTE_TRANSMETTRE' THEN p_acteur_id  ELSE a.analyste_id END,
               a.analyste_nom  = CASE WHEN p_action = 'ANALYSTE_TRANSMETTRE' THEN p_acteur_nom ELSE a.analyste_nom END,
               a.date_analyste = CASE WHEN p_action = 'ANALYSTE_TRANSMETTRE' THEN SYSDATE
                                      WHEN v_nouveau = 'EN_ANALYSE' THEN NULL ELSE a.date_analyste END,
               a.chef_id       = CASE WHEN p_action = 'CHEF_VALIDER' THEN p_acteur_id
                                      WHEN v_nouveau = 'EN_ANALYSE' THEN NULL ELSE a.chef_id END,
               a.chef_nom      = CASE WHEN p_action = 'CHEF_VALIDER' THEN p_acteur_nom
                                      WHEN v_nouveau = 'EN_ANALYSE' THEN NULL ELSE a.chef_nom END,
               a.date_chef     = CASE WHEN p_action = 'CHEF_VALIDER' THEN SYSDATE
                                      WHEN v_nouveau = 'EN_ANALYSE' THEN NULL ELSE a.date_chef END,
               a.directeur_id  = CASE WHEN p_action = 'DIRECTEUR_APPROUVER' THEN p_acteur_id  ELSE a.directeur_id END,
               a.directeur_nom = CASE WHEN p_action = 'DIRECTEUR_APPROUVER' THEN p_acteur_nom ELSE a.directeur_nom END,
               a.date_directeur = CASE WHEN p_action = 'DIRECTEUR_APPROUVER' THEN SYSDATE ELSE a.date_directeur END,
               a.date_modification = SYSDATE
         WHERE a.id_avis = p_id_avis;

        SELECT VALUE(e) INTO v_etude FROM tb_ip_etude e WHERE e.id_etude = v.id_etude;
        journaliser(v_etude.id_fiche, v_etude.phase, 'AVIS_' || p_action, v.statut, v_nouveau,
                    p_commentaire, p_acteur_id, p_acteur_nom);

        -- L'approbation du Directeur applique la décision à l'étude
        IF p_action = 'DIRECTEUR_APPROUVER' THEN
            appliquer_transition(v.id_etude,
                CASE v.decision WHEN 'ACCEPTE' THEN 'DIP_APPROUVER'
                                WHEN 'A_REVISER' THEN 'DIP_COMPLEMENT'
                                ELSE 'DIP_REJETER' END,
                NVL(DBMS_LOB.SUBSTR(v.recommandations, 4000, 1),
                    CASE WHEN v.decision = 'ACCEPTE' THEN 'L''étude est acceptée.' END),
                p_acteur_id, p_acteur_nom);
        END IF;
    END action_avis;

END pkg_ip_etude;
/
