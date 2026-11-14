-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 2
-- 10_v2_package.sql : API PL/SQL (remplace 05_package.sql)
--
-- Circuit de la fiche :
--   BROUILLON --SOUMETTRE--> SOUMISE_UEP --UEP_APPROUVER--> TRANSMISE_DIP (« soumise au MPCE »)
--   SOUMISE_UEP --UEP_COMPLEMENT--> COMPLEMENT_UEP --SOUMETTRE--> SOUMISE_UEP
--   SOUMISE_UEP --UEP_REJETER--> REJETEE_UEP
--   TRANSMISE_DIP : la fiche n'est plus modifiable ; une GRILLE D'ANALYSE est générée
--                   automatiquement (nouvelle grille à chaque arrivée, sans écraser les anciennes).
--   L'approbation de la grille par le Directeur applique son résultat à la fiche :
--     ACCEPTE   -> APPROUVEE_DIP  (validée : plus modifiable)
--     A_REVISER -> COMPLEMENT_DIP (à corriger : modifiable, puis SOUMETTRE -> TRANSMISE_DIP)
--     REJETE    -> REJETEE_DIP
--
-- Circuit de la grille :
--   EN_ANALYSE --ANALYSTE_TRANSMETTRE--> AU_CHEF_SERVICE --CHEF_VALIDER--> AU_DIRECTEUR
--   AU_CHEF_SERVICE --CHEF_RENVOYER--> EN_ANALYSE
--   AU_DIRECTEUR --DIRECTEUR_APPROUVER--> APPROUVEE    --DIRECTEUR_RENVOYER--> EN_ANALYSE
--
-- Codes d'erreur (ORA-20xxx, message affiché à l'utilisateur) :
--   20001 fiche introuvable           20008 suppression impossible
--   20002 fiche non modifiable        20009 action inconnue
--   20003 fiche incomplète            20010 fichier trop volumineux / vide
--   20004 action invalide (statut)    20011 grille incomplète
--   20005 commentaire obligatoire     20012 localisation incohérente
--   20006 nombre maximal d'annexes    20013 grille non modifiable / introuvable
--   20007 idée/annexe introuvable     20014 réponse invalide
-- =============================================================================

CREATE OR REPLACE PACKAGE pkg_ip_identification AS

    c_max_annexes     CONSTANT PLS_INTEGER := 3;
    c_taille_max_octs CONSTANT PLS_INTEGER := 10 * 1024 * 1024;  -- 10 Mo

    -- ------------------------------------------------------------- Fiche
    PROCEDURE creer_fiche (
        p_promoteur_id  IN  VARCHAR2,
        p_promoteur_nom IN  VARCHAR2,
        p_ministere     IN  VARCHAR2,
        p_courriel      IN  VARCHAR2,
        p_id_fiche      OUT NUMBER);

    -- Sections 1.1, 1.3, 1.4, 2.1, 2.2, 4.1
    PROCEDURE maj_fiche (
        p_id_fiche             IN NUMBER,
        p_nom_organisation     IN VARCHAR2,
        p_nom_contact          IN VARCHAR2,
        p_telephone            IN VARCHAR2,
        p_courriel             IN VARCHAR2,
        p_description_probleme IN CLOB,
        p_objectif_general     IN CLOB,
        p_id_departement       IN NUMBER,
        p_id_arrondissement    IN NUMBER,
        p_id_commune           IN NUMBER,
        p_id_section           IN NUMBER,
        p_localite             IN VARCHAR2,
        p_milieu               IN VARCHAR2,
        p_donnees_probleme     IN CLOB,     -- données quantitatives
        p_donnees_qualitatives IN CLOB,
        p_consequences         IN CLOB,
        p_conclusions          IN CLOB);

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

    PROCEDURE definir_meilleure_idee (
        p_id_fiche      IN NUMBER,
        p_id_idee       IN NUMBER,
        p_justification IN CLOB,
        p_titre_projet  IN VARCHAR2);

    PROCEDURE ajouter_annexe (
        p_id_fiche    IN  NUMBER,
        p_nom_fichier IN  VARCHAR2,
        p_type_mime   IN  VARCHAR2,
        p_contenu     IN  BLOB,
        p_description IN  VARCHAR2,
        p_ajoute_par  IN  VARCHAR2,
        p_id_annexe   OUT NUMBER);

    PROCEDURE supprimer_annexe (p_id_fiche IN NUMBER, p_id_annexe IN NUMBER);

    -- Actions du promoteur et de l'UEP (SOUMETTRE, UEP_*).
    -- Les décisions du MPCE passent obligatoirement par la grille d'analyse.
    PROCEDURE changer_statut (
        p_id_fiche    IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2);

    PROCEDURE supprimer_fiche (p_id_fiche IN NUMBER);

    -- ------------------------------------------------- Grille d'analyse
    PROCEDURE maj_grille (
        p_id_grille                IN NUMBER,
        p_commentaires_description IN CLOB,
        p_commentaires_generaux    IN CLOB,
        p_decision                 IN VARCHAR2,
        p_recommandations          IN CLOB);

    -- Généralités : p_reponse = 'OUI' | 'NON' | NULL ; commentaire conservé seulement si NON
    PROCEDURE repondre (
        p_id_grille   IN NUMBER,
        p_numero      IN NUMBER,
        p_reponse     IN VARCHAR2,
        p_commentaire IN VARCHAR2);

    PROCEDURE action_grille (
        p_id_grille   IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2);

    -- Génère une grille pour une fiche au MPCE qui n'en a pas en cours
    -- (utilisé par la migration v1 -> v2 ; sinon appelé automatiquement)
    PROCEDURE generer_grille (p_id_fiche IN NUMBER);

END pkg_ip_identification;
/

CREATE OR REPLACE PACKAGE BODY pkg_ip_identification AS

    -- =========================================================================
    -- Utilitaires privés
    -- =========================================================================
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

    FUNCTION charger_grille (p_id_grille IN NUMBER) RETURN t_ip_grille IS
        v_grille t_ip_grille;
    BEGIN
        SELECT VALUE(g) INTO v_grille FROM tb_ip_grille g
         WHERE g.id_grille = p_id_grille FOR UPDATE;
        RETURN v_grille;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20013, 'Grille d''analyse introuvable.');
    END charger_grille;

    PROCEDURE verifier_modifiable (p_id_fiche IN NUMBER) IS
        v_fiche t_ip_fiche := charger(p_id_fiche, TRUE);
    BEGIN
        IF v_fiche.est_modifiable() = 0 THEN
            RAISE_APPLICATION_ERROR(-20002,
                'La fiche ' || v_fiche.code_fiche || ' n''est pas modifiable dans son statut actuel.');
        END IF;
    END verifier_modifiable;

    PROCEDURE verifier_grille_en_analyse (p_id_grille IN NUMBER) IS
        v_grille t_ip_grille := charger_grille(p_id_grille);
    BEGIN
        IF v_grille.statut <> 'EN_ANALYSE' THEN
            RAISE_APPLICATION_ERROR(-20013,
                'La grille n''est plus modifiable : elle a été transmise pour validation.');
        END IF;
    END verifier_grille_en_analyse;

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

    FUNCTION libelle_lieu (p_id IN NUMBER) RETURN VARCHAR2 IS
        v_lib tb_ip_lieu.libelle%TYPE;
    BEGIN
        IF p_id IS NULL THEN RETURN NULL; END IF;
        SELECT l.libelle INTO v_lib FROM tb_ip_lieu l WHERE l.id_lieu = p_id;
        RETURN v_lib;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN RETURN NULL;
    END libelle_lieu;

    -- Chaque niveau renseigné doit appartenir au niveau supérieur
    PROCEDURE verifier_lieu (p_id IN NUMBER, p_niveau IN VARCHAR2, p_parent IN NUMBER) IS
        v_nb PLS_INTEGER;
    BEGIN
        IF p_id IS NULL THEN RETURN; END IF;
        SELECT COUNT(*) INTO v_nb FROM tb_ip_lieu l
         WHERE l.id_lieu = p_id AND l.niveau = p_niveau
           AND (p_niveau = 'DEPARTEMENT' OR l.id_parent = p_parent);
        IF v_nb = 0 THEN
            RAISE_APPLICATION_ERROR(-20012,
                'Localisation incohérente : vérifiez le département, l''arrondissement, la commune et la section communale.');
        END IF;
    END verifier_lieu;

    -- =========================================================================
    -- Fiche
    -- =========================================================================
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
            SYSDATE, SYSDATE, NULL,
            NULL, NULL,                                           -- objectif, qualitatives
            t_ip_localisation(NULL, NULL, NULL, NULL, NULL, NULL)));

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
        p_objectif_general     IN CLOB,
        p_id_departement       IN NUMBER,
        p_id_arrondissement    IN NUMBER,
        p_id_commune           IN NUMBER,
        p_id_section           IN NUMBER,
        p_localite             IN VARCHAR2,
        p_milieu               IN VARCHAR2,
        p_donnees_probleme     IN CLOB,
        p_donnees_qualitatives IN CLOB,
        p_consequences         IN CLOB,
        p_conclusions          IN CLOB)
    IS
    BEGIN
        verifier_modifiable(p_id_fiche);

        verifier_lieu(p_id_departement, 'DEPARTEMENT', NULL);
        IF p_id_arrondissement IS NOT NULL AND p_id_departement IS NULL
           OR p_id_commune IS NOT NULL AND p_id_arrondissement IS NULL
           OR p_id_section IS NOT NULL AND p_id_commune IS NULL THEN
            RAISE_APPLICATION_ERROR(-20012, 'Localisation incomplète : renseignez les niveaux supérieurs.');
        END IF;
        verifier_lieu(p_id_arrondissement, 'ARRONDISSEMENT', p_id_departement);
        verifier_lieu(p_id_commune, 'COMMUNE', p_id_arrondissement);
        verifier_lieu(p_id_section, 'SECTION', p_id_commune);

        UPDATE tb_ip_fiche f
           SET f.contact = t_ip_contact(p_nom_organisation, p_nom_contact,
                                        p_telephone, p_courriel),
               f.description_probleme = p_description_probleme,
               f.objectif_general     = p_objectif_general,
               f.localisation         = t_ip_localisation(p_id_departement, p_id_arrondissement,
                                                          p_id_commune, p_id_section,
                                                          p_localite, p_milieu),
               f.donnees_probleme     = p_donnees_probleme,
               f.donnees_qualitatives = p_donnees_qualitatives,
               f.consequences         = p_consequences,
               f.conclusions          = p_conclusions,
               f.date_modification    = SYSDATE
         WHERE f.id_fiche = p_id_fiche;
    END maj_fiche;

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

        UPDATE tb_ip_fiche f
           SET f.justification_choix = CASE WHEN p_id_idee IS NOT NULL THEN p_justification END,
               f.titre_projet        = CASE WHEN p_id_idee IS NOT NULL THEN p_titre_projet END,
               f.date_modification   = SYSDATE
         WHERE f.id_fiche = p_id_fiche;
    END definir_meilleure_idee;

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
        verifier_modifiable(p_id_fiche);

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

    -- =========================================================================
    -- Grille d'analyse : génération (copie de la fiche telle que soumise)
    -- =========================================================================
    PROCEDURE generer_grille (p_id_fiche IN NUMBER) IS
        v_fiche       t_ip_fiche := charger(p_id_fiche);
        v_numero      NUMBER;
        v_en_cours    PLS_INTEGER;
        v_date_uep    DATE;
        v_meilleure   t_ip_idee_possible;
        v_pistes      t_ip_piste_tab;
        v_reponses    t_ip_reponse_tab;
        v_id_grille   NUMBER;
        v_loc         t_ip_localisation := v_fiche.localisation;
        v_dep         VARCHAR2(150);
        v_arr         VARCHAR2(150);
        v_com         VARCHAR2(150);
        v_sec         VARCHAR2(150);
    BEGIN
        IF v_fiche.statut <> 'TRANSMISE_DIP' THEN
            RAISE_APPLICATION_ERROR(-20004,
                'Une grille ne peut être générée que pour une fiche soumise au MPCE.');
        END IF;
        SELECT COUNT(*) INTO v_en_cours FROM tb_ip_grille g
         WHERE g.id_fiche = p_id_fiche AND g.statut <> 'APPROUVEE';
        IF v_en_cours > 0 THEN
            RETURN;   -- une grille est déjà en cours pour cette soumission
        END IF;

        SELECT NVL(MAX(g.numero), 0) + 1 INTO v_numero
          FROM tb_ip_grille g WHERE g.id_fiche = p_id_fiche;

        SELECT MAX(h.date_action) INTO v_date_uep
          FROM tb_ip_historique h
         WHERE h.id_fiche = p_id_fiche AND h.action = 'UEP_APPROUVER';

        BEGIN
            SELECT VALUE(i) INTO v_meilleure FROM tb_ip_idee_possible i
             WHERE i.id_fiche = p_id_fiche AND i.est_meilleure = 1;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                v_meilleure := t_ip_idee_possible(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0);
        END;

        -- Libellés de la localisation (fonction PL/SQL privée : hors de l'ordre SQL)
        v_dep := libelle_lieu(v_loc.id_departement);
        v_arr := libelle_lieu(v_loc.id_arrondissement);
        v_com := libelle_lieu(v_loc.id_commune);
        v_sec := libelle_lieu(v_loc.id_section);

        SELECT t_ip_piste(i.ordre, i.intitule, i.description, i.est_meilleure)
          BULK COLLECT INTO v_pistes
          FROM tb_ip_idee_possible i
         WHERE i.id_fiche = p_id_fiche
         ORDER BY i.ordre, i.id_idee;

        SELECT t_ip_reponse(q.numero, q.libelle, NULL, NULL)
          BULK COLLECT INTO v_reponses
          FROM tb_ip_question q
         WHERE q.actif = 1
         ORDER BY q.numero;

        v_id_grille := seq_ip_grille.NEXTVAL;
        INSERT INTO tb_ip_grille VALUES (t_ip_grille(
            v_id_grille, p_id_fiche, v_numero, 'EN_ANALYSE',
            NVL(v_fiche.contact.nom_organisation, v_fiche.ministere),
            v_meilleure.intitule,
            v_date_uep, SYSDATE,
            v_dep, v_arr, v_com, v_sec,
            v_loc.localite, v_loc.milieu,
            v_fiche.description_probleme, v_fiche.objectif_general,
            v_fiche.donnees_probleme, v_fiche.donnees_qualitatives,
            v_pistes,
            v_meilleure.intitule, v_meilleure.legitimite, v_meilleure.pertinence, v_meilleure.viabilite,
            NULL,                                   -- commentaires sur la description
            v_reponses, NULL, NULL, NULL,           -- réponses, commentaires, décision, recommandations
            NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
            SYSDATE, SYSDATE));

        journaliser(p_id_fiche, 'GRILLE_GENEREE', NULL, 'EN_ANALYSE',
                    'Grille d''analyse n° ' || v_numero || ' générée.', NULL, 'Système');
    END generer_grille;

    -- =========================================================================
    -- Transitions de la fiche
    -- =========================================================================
    PROCEDURE appliquer_transition (
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
                    'Un commentaire est obligatoire pour demander une correction ou rejeter la fiche.');
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
                -- Une fiche « à corriger » par le MPCE y retourne directement
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

        -- Chaque arrivée au MPCE produit une nouvelle grille d'analyse
        IF v_nouveau = 'TRANSMISE_DIP' THEN
            generer_grille(p_id_fiche);
        END IF;
    END appliquer_transition;

    PROCEDURE changer_statut (
        p_id_fiche    IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2)
    IS
    BEGIN
        IF p_action LIKE 'DIP\_%' ESCAPE '\' THEN
            RAISE_APPLICATION_ERROR(-20004,
                'La décision du MPCE est prise par l''approbation de la grille d''analyse.');
        END IF;
        appliquer_transition(p_id_fiche, p_action, p_commentaire, p_acteur_id, p_acteur_nom);
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

    -- =========================================================================
    -- Grille d'analyse : saisie et circuit de validation
    -- =========================================================================
    PROCEDURE maj_grille (
        p_id_grille                IN NUMBER,
        p_commentaires_description IN CLOB,
        p_commentaires_generaux    IN CLOB,
        p_decision                 IN VARCHAR2,
        p_recommandations          IN CLOB)
    IS
    BEGIN
        verifier_grille_en_analyse(p_id_grille);
        UPDATE tb_ip_grille g
           SET g.commentaires_description = p_commentaires_description,
               g.commentaires_generaux    = p_commentaires_generaux,
               g.decision                 = p_decision,
               g.recommandations          = p_recommandations,
               g.date_modification        = SYSDATE
         WHERE g.id_grille = p_id_grille;
    END maj_grille;

    PROCEDURE repondre (
        p_id_grille   IN NUMBER,
        p_numero      IN NUMBER,
        p_reponse     IN VARCHAR2,
        p_commentaire IN VARCHAR2)
    IS
    BEGIN
        IF p_reponse IS NOT NULL AND p_reponse NOT IN ('OUI', 'NON') THEN
            RAISE_APPLICATION_ERROR(-20014, 'Réponse invalide : choisissez Oui ou Non.');
        END IF;
        verifier_grille_en_analyse(p_id_grille);

        UPDATE TABLE (SELECT g.reponses FROM tb_ip_grille g WHERE g.id_grille = p_id_grille) r
           SET r.reponse     = p_reponse,
               r.commentaire = CASE WHEN p_reponse = 'NON' THEN SUBSTR(p_commentaire, 1, 2000) END
         WHERE r.numero = p_numero;
        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(-20014, 'Question ' || p_numero || ' introuvable dans la grille.');
        END IF;

        UPDATE tb_ip_grille g SET g.date_modification = SYSDATE WHERE g.id_grille = p_id_grille;
    END repondre;

    PROCEDURE action_grille (
        p_id_grille   IN NUMBER,
        p_action      IN VARCHAR2,
        p_commentaire IN VARCHAR2,
        p_acteur_id   IN VARCHAR2,
        p_acteur_nom  IN VARCHAR2)
    IS
        v_grille   t_ip_grille := charger_grille(p_id_grille);
        v_attendu  VARCHAR2(20);
        v_nouveau  VARCHAR2(20);
        v_anomalie VARCHAR2(400);
        v_action_fiche VARCHAR2(30);

        PROCEDURE exiger_commentaire IS
        BEGIN
            IF TRIM(p_commentaire) IS NULL THEN
                RAISE_APPLICATION_ERROR(-20005,
                    'Un commentaire est obligatoire pour renvoyer la grille à l''analyste.');
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

        IF v_grille.statut <> v_attendu THEN
            RAISE_APPLICATION_ERROR(-20004,
                'Action impossible : la grille est au statut ' || v_grille.statut || '.');
        END IF;

        IF p_action = 'ANALYSTE_TRANSMETTRE' THEN
            v_anomalie := v_grille.anomalie();
            IF v_anomalie IS NOT NULL THEN
                RAISE_APPLICATION_ERROR(-20011, 'Grille incomplète : ' || v_anomalie);
            END IF;
        END IF;

        UPDATE tb_ip_grille g
           SET g.statut        = v_nouveau,
               g.analyste_id   = CASE WHEN p_action = 'ANALYSTE_TRANSMETTRE' THEN p_acteur_id  ELSE g.analyste_id END,
               g.analyste_nom  = CASE WHEN p_action = 'ANALYSTE_TRANSMETTRE' THEN p_acteur_nom ELSE g.analyste_nom END,
               g.date_analyste = CASE WHEN p_action = 'ANALYSTE_TRANSMETTRE' THEN SYSDATE
                                      WHEN v_nouveau = 'EN_ANALYSE' THEN NULL ELSE g.date_analyste END,
               g.chef_id       = CASE WHEN p_action = 'CHEF_VALIDER' THEN p_acteur_id
                                      WHEN v_nouveau = 'EN_ANALYSE' THEN NULL ELSE g.chef_id END,
               g.chef_nom      = CASE WHEN p_action = 'CHEF_VALIDER' THEN p_acteur_nom
                                      WHEN v_nouveau = 'EN_ANALYSE' THEN NULL ELSE g.chef_nom END,
               g.date_chef     = CASE WHEN p_action = 'CHEF_VALIDER' THEN SYSDATE
                                      WHEN v_nouveau = 'EN_ANALYSE' THEN NULL ELSE g.date_chef END,
               g.directeur_id  = CASE WHEN p_action = 'DIRECTEUR_APPROUVER' THEN p_acteur_id  ELSE g.directeur_id END,
               g.directeur_nom = CASE WHEN p_action = 'DIRECTEUR_APPROUVER' THEN p_acteur_nom ELSE g.directeur_nom END,
               g.date_directeur = CASE WHEN p_action = 'DIRECTEUR_APPROUVER' THEN SYSDATE ELSE g.date_directeur END,
               g.date_modification = SYSDATE
         WHERE g.id_grille = p_id_grille;

        journaliser(v_grille.id_fiche, 'GRILLE_' || p_action, v_grille.statut, v_nouveau,
                    p_commentaire, p_acteur_id, p_acteur_nom);

        -- L'approbation du Directeur applique le résultat de l'analyse à la fiche
        IF p_action = 'DIRECTEUR_APPROUVER' THEN
            v_action_fiche := CASE v_grille.decision
                                  WHEN 'ACCEPTE'   THEN 'DIP_APPROUVER'
                                  WHEN 'A_REVISER' THEN 'DIP_COMPLEMENT'
                                  WHEN 'REJETE'    THEN 'DIP_REJETER' END;
            appliquer_transition(v_grille.id_fiche, v_action_fiche,
                                 NVL(DBMS_LOB.SUBSTR(v_grille.recommandations, 4000, 1),
                                     CASE WHEN v_grille.decision = 'ACCEPTE'
                                          THEN 'Le document de l''idée de projet est accepté.' END),
                                 p_acteur_id, p_acteur_nom);
        END IF;
    END action_grille;

END pkg_ip_identification;
/
