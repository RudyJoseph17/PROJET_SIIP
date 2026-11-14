-- =============================================================================
-- SIIP - Module "Identification projet"
-- 05_package.sql : API PL/SQL (toutes les écritures + circuit de validation)
--
-- Circuit (Manuel de procédures, §3.E et Graphique 7) :
--   BROUILLON --SOUMETTRE--> SOUMISE_UEP
--   SOUMISE_UEP --UEP_APPROUVER--> TRANSMISE_DIP
--               --UEP_COMPLEMENT--> COMPLEMENT_UEP --SOUMETTRE--> SOUMISE_UEP
--               --UEP_REJETER--> REJETEE_UEP
--   TRANSMISE_DIP --DIP_APPROUVER--> APPROUVEE_DIP (passage à l'Avant-Projet)
--                 --DIP_COMPLEMENT--> COMPLEMENT_DIP --SOUMETTRE--> TRANSMISE_DIP
--                 --DIP_REJETER--> REJETEE_DIP
--
-- Codes d'erreur (ORA-20xxx, message affiché à l'utilisateur) :
--   20001 fiche introuvable           20006 nombre maximal d'annexes atteint
--   20002 fiche non modifiable        20007 idée/annexe introuvable
--   20003 fiche incomplète            20008 suppression impossible
--   20004 action invalide (statut)    20009 action inconnue
--   20005 commentaire obligatoire     20010 fichier trop volumineux
-- =============================================================================

CREATE OR REPLACE PACKAGE pkg_ip_identification AS

    c_max_annexes     CONSTANT PLS_INTEGER := 3;
    c_taille_max_octs CONSTANT PLS_INTEGER := 10 * 1024 * 1024;  -- 10 Mo

    PROCEDURE creer_fiche (
        p_promoteur_id  IN  VARCHAR2,
        p_promoteur_nom IN  VARCHAR2,
        p_ministere     IN  VARCHAR2,
        p_courriel      IN  VARCHAR2,
        p_id_fiche      OUT NUMBER);

    -- Sections 1.1, 1.3, 2.1, 2.2, 4.1
    PROCEDURE maj_fiche (
        p_id_fiche             IN NUMBER,
        p_nom_organisation     IN VARCHAR2,
        p_nom_contact          IN VARCHAR2,
        p_telephone            IN VARCHAR2,
        p_courriel             IN VARCHAR2,
        p_description_probleme IN CLOB,
        p_donnees_probleme     IN CLOB,
        p_consequences         IN CLOB,
        p_conclusions          IN CLOB);

    -- Sections 3.1 à 3.4 (p_id_idee NULL = nouvelle idée)
    PROCEDURE enregistrer_idee (
        p_id_fiche    IN     NUMBER,
        p_id_idee     IN OUT NUMBER,
        p_ordre       IN     NUMBER,
        p_intitule    IN     VARCHAR2,
        p_description IN     CLOB,
        p_legitimite  IN     CLOB,
        p_pertinence  IN     CLOB,
        p_viabilite   IN     CLOB);

    PROCEDURE supprimer_idee (p_id_fiche IN NUMBER, p_id_idee IN NUMBER);

    -- Section 3.5 + 1.2 : le titre n'est conservé que si une meilleure idée est retenue
    PROCEDURE definir_meilleure_idee (
        p_id_fiche      IN NUMBER,
        p_id_idee       IN NUMBER,     -- NULL = aucune
        p_justification IN CLOB,
        p_titre_projet  IN VARCHAR2);

    -- Section 5
    PROCEDURE ajouter_annexe (
        p_id_fiche    IN  NUMBER,
        p_nom_fichier IN  VARCHAR2,
        p_type_mime   IN  VARCHAR2,
        p_contenu     IN  BLOB,
        p_description IN  VARCHAR2,
        p_ajoute_par  IN  VARCHAR2,
        p_id_annexe   OUT NUMBER);

    PROCEDURE supprimer_annexe (p_id_fiche IN NUMBER, p_id_annexe IN NUMBER);

    -- Circuit de validation
    PROCEDURE changer_statut (
        p_id_fiche    IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2);

    PROCEDURE supprimer_fiche (p_id_fiche IN NUMBER);

END pkg_ip_identification;
/

CREATE OR REPLACE PACKAGE BODY pkg_ip_identification AS

    -- -------------------------------------------------------------------------
    -- Utilitaires privés
    -- -------------------------------------------------------------------------
    FUNCTION charger (p_id_fiche IN NUMBER, p_verrou IN BOOLEAN := FALSE)
        RETURN t_ip_fiche
    IS
        v_fiche t_ip_fiche;
    BEGIN
        IF p_verrou THEN
            SELECT VALUE(f) INTO v_fiche FROM tb_ip_fiche f
             WHERE f.id_fiche = p_id_fiche FOR UPDATE;
        ELSE
            SELECT VALUE(f) INTO v_fiche FROM tb_ip_fiche f
             WHERE f.id_fiche = p_id_fiche;
        END IF;
        RETURN v_fiche;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20001, 'Fiche d''identification introuvable.');
    END charger;

    PROCEDURE verifier_modifiable (p_id_fiche IN NUMBER) IS
        v_fiche t_ip_fiche := charger(p_id_fiche, TRUE);
    BEGIN
        IF v_fiche.est_modifiable() = 0 THEN
            RAISE_APPLICATION_ERROR(-20002,
                'La fiche ' || v_fiche.code_fiche || ' n''est pas modifiable dans son statut actuel.');
        END IF;
    END verifier_modifiable;

    PROCEDURE toucher (p_id_fiche IN NUMBER) IS
    BEGIN
        UPDATE tb_ip_fiche f SET f.date_modification = SYSDATE
         WHERE f.id_fiche = p_id_fiche;
    END toucher;

    PROCEDURE journaliser (
        p_id_fiche IN NUMBER, p_action IN VARCHAR2,
        p_ancien IN VARCHAR2, p_nouveau IN VARCHAR2, p_commentaire IN VARCHAR2,
        p_acteur_id IN VARCHAR2, p_acteur_nom IN VARCHAR2)
    IS
    BEGIN
        INSERT INTO tb_ip_historique
        VALUES (t_ip_historique(seq_ip_historique.NEXTVAL, p_id_fiche, p_action,
                                p_ancien, p_nouveau, SUBSTR(p_commentaire, 1, 4000),
                                p_acteur_id, p_acteur_nom, SYSDATE));
    END journaliser;

    -- -------------------------------------------------------------------------
    -- Fiche
    -- -------------------------------------------------------------------------
    PROCEDURE creer_fiche (
        p_promoteur_id  IN  VARCHAR2,
        p_promoteur_nom IN  VARCHAR2,
        p_ministere     IN  VARCHAR2,
        p_courriel      IN  VARCHAR2,
        p_id_fiche      OUT NUMBER)
    IS
    BEGIN
        p_id_fiche := seq_ip_fiche.NEXTVAL;
        INSERT INTO tb_ip_fiche
        VALUES (t_ip_fiche(
            p_id_fiche,
            'IP-' || TO_CHAR(SYSDATE, 'YYYY') || '-' || LPAD(p_id_fiche, 5, '0'),
            t_ip_contact(p_ministere, p_promoteur_nom, NULL, p_courriel),
            NULL, NULL, NULL, NULL, NULL, NULL,
            'BROUILLON', p_promoteur_id, p_promoteur_nom, p_ministere,
            SYSDATE, SYSDATE, NULL));

        journaliser(p_id_fiche, 'CREATION', NULL, 'BROUILLON', NULL,
                    p_promoteur_id, p_promoteur_nom);
    END creer_fiche;

    PROCEDURE maj_fiche (
        p_id_fiche             IN NUMBER,
        p_nom_organisation     IN VARCHAR2,
        p_nom_contact          IN VARCHAR2,
        p_telephone            IN VARCHAR2,
        p_courriel             IN VARCHAR2,
        p_description_probleme IN CLOB,
        p_donnees_probleme     IN CLOB,
        p_consequences         IN CLOB,
        p_conclusions          IN CLOB)
    IS
    BEGIN
        verifier_modifiable(p_id_fiche);
        UPDATE tb_ip_fiche f
           SET f.contact = t_ip_contact(p_nom_organisation, p_nom_contact,
                                        p_telephone, p_courriel),
               f.description_probleme = p_description_probleme,
               f.donnees_probleme     = p_donnees_probleme,
               f.consequences         = p_consequences,
               f.conclusions          = p_conclusions,
               f.date_modification    = SYSDATE
         WHERE f.id_fiche = p_id_fiche;
    END maj_fiche;

    -- -------------------------------------------------------------------------
    -- Idées possibles
    -- -------------------------------------------------------------------------
    PROCEDURE enregistrer_idee (
        p_id_fiche    IN     NUMBER,
        p_id_idee     IN OUT NUMBER,
        p_ordre       IN     NUMBER,
        p_intitule    IN     VARCHAR2,
        p_description IN     CLOB,
        p_legitimite  IN     CLOB,
        p_pertinence  IN     CLOB,
        p_viabilite   IN     CLOB)
    IS
    BEGIN
        verifier_modifiable(p_id_fiche);

        IF p_id_idee IS NULL THEN
            p_id_idee := seq_ip_idee.NEXTVAL;
            INSERT INTO tb_ip_idee_possible
            VALUES (t_ip_idee_possible(p_id_idee, p_id_fiche, p_ordre, p_intitule,
                                       p_description, p_legitimite, p_pertinence,
                                       p_viabilite, 0));
        ELSE
            UPDATE tb_ip_idee_possible i
               SET i.ordre       = p_ordre,
                   i.intitule    = p_intitule,
                   i.description = p_description,
                   i.legitimite  = p_legitimite,
                   i.pertinence  = p_pertinence,
                   i.viabilite   = p_viabilite
             WHERE i.id_idee = p_id_idee AND i.id_fiche = p_id_fiche;
            IF SQL%ROWCOUNT = 0 THEN
                RAISE_APPLICATION_ERROR(-20007, 'Idée possible introuvable pour cette fiche.');
            END IF;
        END IF;
        toucher(p_id_fiche);
    END enregistrer_idee;

    PROCEDURE supprimer_idee (p_id_fiche IN NUMBER, p_id_idee IN NUMBER) IS
    BEGIN
        verifier_modifiable(p_id_fiche);
        DELETE FROM tb_ip_idee_possible i
         WHERE i.id_idee = p_id_idee AND i.id_fiche = p_id_fiche;
        toucher(p_id_fiche);
    END supprimer_idee;

    PROCEDURE definir_meilleure_idee (
        p_id_fiche      IN NUMBER,
        p_id_idee       IN NUMBER,
        p_justification IN CLOB,
        p_titre_projet  IN VARCHAR2)
    IS
    BEGIN
        verifier_modifiable(p_id_fiche);

        UPDATE tb_ip_idee_possible i SET i.est_meilleure = 0
         WHERE i.id_fiche = p_id_fiche AND i.est_meilleure = 1;

        IF p_id_idee IS NOT NULL THEN
            UPDATE tb_ip_idee_possible i SET i.est_meilleure = 1
             WHERE i.id_idee = p_id_idee AND i.id_fiche = p_id_fiche;
            IF SQL%ROWCOUNT = 0 THEN
                RAISE_APPLICATION_ERROR(-20007, 'Idée possible introuvable pour cette fiche.');
            END IF;
        END IF;

        -- 1.2 n'existe qu'après la détermination de la meilleure idée
        UPDATE tb_ip_fiche f
           SET f.justification_choix = CASE WHEN p_id_idee IS NOT NULL THEN p_justification END,
               f.titre_projet        = CASE WHEN p_id_idee IS NOT NULL THEN p_titre_projet END,
               f.date_modification   = SYSDATE
         WHERE f.id_fiche = p_id_fiche;
    END definir_meilleure_idee;

    -- -------------------------------------------------------------------------
    -- Annexes
    -- -------------------------------------------------------------------------
    PROCEDURE ajouter_annexe (
        p_id_fiche    IN  NUMBER,
        p_nom_fichier IN  VARCHAR2,
        p_type_mime   IN  VARCHAR2,
        p_contenu     IN  BLOB,
        p_description IN  VARCHAR2,
        p_ajoute_par  IN  VARCHAR2,
        p_id_annexe   OUT NUMBER)
    IS
        v_nb     PLS_INTEGER;
        v_taille NUMBER := NVL(DBMS_LOB.GETLENGTH(p_contenu), 0);
    BEGIN
        verifier_modifiable(p_id_fiche);   -- verrouille la fiche

        SELECT COUNT(*) INTO v_nb FROM tb_ip_annexe a WHERE a.id_fiche = p_id_fiche;
        IF v_nb >= c_max_annexes THEN
            RAISE_APPLICATION_ERROR(-20006,
                'Une fiche ne peut pas avoir plus de ' || c_max_annexes || ' documents annexes.');
        END IF;
        IF v_taille = 0 THEN
            RAISE_APPLICATION_ERROR(-20010, 'Le fichier est vide.');
        END IF;
        IF v_taille > c_taille_max_octs THEN
            RAISE_APPLICATION_ERROR(-20010, 'Le fichier dépasse la taille maximale de 10 Mo.');
        END IF;

        p_id_annexe := seq_ip_annexe.NEXTVAL;
        INSERT INTO tb_ip_annexe
        VALUES (t_ip_annexe(p_id_annexe, p_id_fiche, p_nom_fichier, p_type_mime,
                            v_taille, p_contenu, p_description, SYSDATE, p_ajoute_par));
        toucher(p_id_fiche);
    END ajouter_annexe;

    PROCEDURE supprimer_annexe (p_id_fiche IN NUMBER, p_id_annexe IN NUMBER) IS
    BEGIN
        verifier_modifiable(p_id_fiche);
        DELETE FROM tb_ip_annexe a
         WHERE a.id_annexe = p_id_annexe AND a.id_fiche = p_id_fiche;
        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(-20007, 'Document annexe introuvable pour cette fiche.');
        END IF;
        toucher(p_id_fiche);
    END supprimer_annexe;

    -- -------------------------------------------------------------------------
    -- Circuit de validation
    -- -------------------------------------------------------------------------
    PROCEDURE changer_statut (
        p_id_fiche    IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2)
    IS
        v_fiche   t_ip_fiche := charger(p_id_fiche, TRUE);
        v_attendu VARCHAR2(20);
        v_nouveau VARCHAR2(20);

        PROCEDURE exiger_commentaire IS
        BEGIN
            IF TRIM(p_commentaire) IS NULL THEN
                RAISE_APPLICATION_ERROR(-20005,
                    'Un commentaire est obligatoire pour demander un complément ou rejeter la fiche.');
            END IF;
        END;
    BEGIN
        CASE p_action
            WHEN 'SOUMETTRE' THEN
                IF v_fiche.est_modifiable() = 0 THEN
                    RAISE_APPLICATION_ERROR(-20004,
                        'La fiche ne peut pas être soumise dans son statut actuel.');
                END IF;
                IF v_fiche.progression() < 100 THEN
                    RAISE_APPLICATION_ERROR(-20003,
                        'La fiche est incomplète (' || v_fiche.progression()
                        || ' %). Complétez toutes les sections avant de la soumettre.');
                END IF;
                v_attendu := v_fiche.statut;
                -- Un complément demandé par la DIP retourne directement à la DIP
                v_nouveau := CASE v_fiche.statut
                                 WHEN 'COMPLEMENT_DIP' THEN 'TRANSMISE_DIP'
                                 ELSE 'SOUMISE_UEP' END;
            WHEN 'UEP_APPROUVER'  THEN v_attendu := 'SOUMISE_UEP';   v_nouveau := 'TRANSMISE_DIP';
            WHEN 'UEP_COMPLEMENT' THEN v_attendu := 'SOUMISE_UEP';   v_nouveau := 'COMPLEMENT_UEP'; exiger_commentaire;
            WHEN 'UEP_REJETER'    THEN v_attendu := 'SOUMISE_UEP';   v_nouveau := 'REJETEE_UEP';    exiger_commentaire;
            WHEN 'DIP_APPROUVER'  THEN v_attendu := 'TRANSMISE_DIP'; v_nouveau := 'APPROUVEE_DIP';
            WHEN 'DIP_COMPLEMENT' THEN v_attendu := 'TRANSMISE_DIP'; v_nouveau := 'COMPLEMENT_DIP'; exiger_commentaire;
            WHEN 'DIP_REJETER'    THEN v_attendu := 'TRANSMISE_DIP'; v_nouveau := 'REJETEE_DIP';    exiger_commentaire;
            ELSE
                RAISE_APPLICATION_ERROR(-20009, 'Action inconnue : ' || p_action);
        END CASE;

        IF v_fiche.statut <> v_attendu THEN
            RAISE_APPLICATION_ERROR(-20004,
                'Action impossible : la fiche est au statut ' || v_fiche.statut || '.');
        END IF;

        UPDATE tb_ip_fiche f
           SET f.statut            = v_nouveau,
               f.date_soumission   = CASE WHEN p_action = 'SOUMETTRE' THEN SYSDATE
                                          ELSE f.date_soumission END,
               f.date_modification = SYSDATE
         WHERE f.id_fiche = p_id_fiche;

        journaliser(p_id_fiche, p_action, v_fiche.statut, v_nouveau, p_commentaire,
                    p_acteur_id, p_acteur_nom);
    END changer_statut;

    PROCEDURE supprimer_fiche (p_id_fiche IN NUMBER) IS
        v_fiche t_ip_fiche := charger(p_id_fiche, TRUE);
    BEGIN
        IF v_fiche.statut <> 'BROUILLON' THEN
            RAISE_APPLICATION_ERROR(-20008,
                'Seule une fiche au statut Brouillon peut être supprimée.');
        END IF;
        DELETE FROM tb_ip_fiche f WHERE f.id_fiche = p_id_fiche;  -- cascade
    END supprimer_fiche;

END pkg_ip_identification;
/
