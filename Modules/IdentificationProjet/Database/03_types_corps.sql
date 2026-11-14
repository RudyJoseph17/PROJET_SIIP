-- =============================================================================
-- SIIP - Module "Identification projet"
-- 03_types_corps.sql : corps des types objet (règles de complétude/progression)
-- Les règles de progression sont définies ici, une seule fois, et exposées
-- à l'application par les vues (04_vues.sql).
-- =============================================================================

CREATE OR REPLACE TYPE BODY t_ip_contact AS
    MEMBER FUNCTION est_complet RETURN NUMBER IS
    BEGIN
        IF TRIM(nom_organisation) IS NOT NULL
           AND TRIM(nom_contact) IS NOT NULL
           AND (TRIM(telephone) IS NOT NULL OR TRIM(courriel) IS NOT NULL) THEN
            RETURN 1;
        END IF;
        RETURN 0;
    END;
END;
/

CREATE OR REPLACE TYPE BODY t_ip_idee_possible AS
    MEMBER FUNCTION est_complete RETURN NUMBER IS
        FUNCTION rempli (p CLOB) RETURN BOOLEAN IS
        BEGIN
            RETURN p IS NOT NULL AND DBMS_LOB.GETLENGTH(p) > 0;
        END;
    BEGIN
        IF TRIM(intitule) IS NOT NULL
           AND rempli(legitimite) AND rempli(pertinence) AND rempli(viabilite) THEN
            RETURN 1;
        END IF;
        RETURN 0;
    END;
END;
/

CREATE OR REPLACE TYPE BODY t_ip_fiche AS

    MEMBER FUNCTION etape_complete (p_etape IN NUMBER) RETURN NUMBER IS
        v_nb_idees      NUMBER;
        v_nb_completes  NUMBER;
        v_nb_meilleures NUMBER;

        FUNCTION rempli (p CLOB) RETURN BOOLEAN IS
        BEGIN
            RETURN p IS NOT NULL AND DBMS_LOB.GETLENGTH(p) > 0;
        END;
    BEGIN
        CASE p_etape
            -- 1. Introduction : 1.1 contact + 1.3 description du problème
            WHEN 1 THEN
                IF contact IS NOT NULL AND contact.est_complet() = 1
                   AND rempli(description_probleme) THEN
                    RETURN 1;
                END IF;

            -- 2. Importance du problème/besoin : 2.1 + 2.2
            WHEN 2 THEN
                IF rempli(donnees_probleme) AND rempli(consequences) THEN
                    RETURN 1;
                END IF;

            -- 3. Idées possibles (3.1-3.4) : au moins une, toutes complètes
            WHEN 3 THEN
                SELECT COUNT(*), NVL(SUM(i.est_complete()), 0)
                  INTO v_nb_idees, v_nb_completes
                  FROM tb_ip_idee_possible i
                 WHERE i.id_fiche = SELF.id_fiche;
                IF v_nb_idees > 0 AND v_nb_idees = v_nb_completes THEN
                    RETURN 1;
                END IF;

            -- 4. Meilleure idée (3.5) + titre proposé (1.2)
            WHEN 4 THEN
                SELECT COUNT(*) INTO v_nb_meilleures
                  FROM tb_ip_idee_possible i
                 WHERE i.id_fiche = SELF.id_fiche AND i.est_meilleure = 1;
                IF v_nb_meilleures = 1 AND rempli(justification_choix)
                   AND TRIM(titre_projet) IS NOT NULL THEN
                    RETURN 1;
                END IF;

            -- 5. Conclusion et recommandations (4.1)
            WHEN 5 THEN
                IF rempli(conclusions) THEN
                    RETURN 1;
                END IF;

            ELSE
                NULL;
        END CASE;
        RETURN 0;
    END etape_complete;

    MEMBER FUNCTION progression RETURN NUMBER IS
        v_total NUMBER := 0;
    BEGIN
        FOR e IN 1 .. 5 LOOP
            v_total := v_total + etape_complete(e);
        END LOOP;
        RETURN ROUND(v_total * 100 / 5);
    END progression;

    MEMBER FUNCTION est_modifiable RETURN NUMBER IS
    BEGIN
        IF statut IN ('BROUILLON', 'COMPLEMENT_UEP', 'COMPLEMENT_DIP') THEN
            RETURN 1;
        END IF;
        RETURN 0;
    END est_modifiable;

END;
/
