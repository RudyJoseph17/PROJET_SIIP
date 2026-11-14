-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 2
-- 08_v2_types_corps.sql : corps des types (remplace la version de 03 pour T_IP_FICHE)
-- =============================================================================

CREATE OR REPLACE TYPE BODY t_ip_localisation AS
    MEMBER FUNCTION est_complete RETURN NUMBER IS
    BEGIN
        IF id_departement IS NOT NULL AND id_arrondissement IS NOT NULL
           AND id_commune IS NOT NULL AND milieu IS NOT NULL THEN
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
            -- 1. Introduction : 1.1 contact, 1.3 problème + objectif général, 1.4 localisation
            WHEN 1 THEN
                IF contact IS NOT NULL AND contact.est_complet() = 1
                   AND rempli(description_probleme) AND rempli(objectif_general)
                   AND localisation IS NOT NULL AND localisation.est_complete() = 1 THEN
                    RETURN 1;
                END IF;

            -- 2. Importance : 2.1 données (quantitatives et/ou qualitatives) + 2.2
            WHEN 2 THEN
                IF (rempli(donnees_probleme) OR rempli(donnees_qualitatives))
                   AND rempli(consequences) THEN
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

    -- Modifiable en brouillon et après une demande de correction (UEP ou MPCE)
    MEMBER FUNCTION est_modifiable RETURN NUMBER IS
    BEGIN
        IF statut IN ('BROUILLON', 'COMPLEMENT_UEP', 'COMPLEMENT_DIP') THEN
            RETURN 1;
        END IF;
        RETURN 0;
    END est_modifiable;

END;
/

CREATE OR REPLACE TYPE BODY t_ip_grille AS

    MEMBER FUNCTION anomalie RETURN VARCHAR2 IS
        FUNCTION rempli (p CLOB) RETURN BOOLEAN IS
        BEGIN
            RETURN p IS NOT NULL AND DBMS_LOB.GETLENGTH(p) > 0;
        END;
    BEGIN
        IF reponses IS NULL OR reponses.COUNT = 0 THEN
            RETURN 'La grille ne contient aucune question.';
        END IF;
        FOR i IN 1 .. reponses.COUNT LOOP
            IF reponses(i).reponse IS NULL THEN
                RETURN 'Répondez par Oui ou Non à la question ' || reponses(i).numero || '.';
            END IF;
            IF reponses(i).reponse = 'NON' AND TRIM(reponses(i).commentaire) IS NULL THEN
                RETURN 'Un commentaire est obligatoire pour la question ' || reponses(i).numero
                       || ' (réponse Non).';
            END IF;
        END LOOP;
        IF decision IS NULL THEN
            RETURN 'Choisissez le résultat de l''analyse.';
        END IF;
        IF decision IN ('A_REVISER', 'REJETE') AND NOT rempli(recommandations) THEN
            RETURN 'Les recommandations sont obligatoires lorsque le document doit être révisé ou est rejeté.';
        END IF;
        RETURN NULL;
    END anomalie;

    MEMBER FUNCTION nb_non RETURN NUMBER IS
        v_nb NUMBER := 0;
    BEGIN
        IF reponses IS NOT NULL THEN
            FOR i IN 1 .. reponses.COUNT LOOP
                IF reponses(i).reponse = 'NON' THEN
                    v_nb := v_nb + 1;
                END IF;
            END LOOP;
        END IF;
        RETURN v_nb;
    END nb_non;

END;
/
