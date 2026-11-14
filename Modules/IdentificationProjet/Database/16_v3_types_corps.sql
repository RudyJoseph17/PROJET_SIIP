-- =============================================================================
-- SIIP - Module "Identification projet" — VERSION 3
-- 16_v3_types_corps.sql : corps des types T_IP_ETUDE et T_IP_AVIS
-- =============================================================================

CREATE OR REPLACE TYPE BODY t_ip_etude AS
    -- Modifiable en brouillon et après une demande de correction (UEP ou MPCE)
    MEMBER FUNCTION est_modifiable RETURN NUMBER IS
    BEGIN
        IF statut IN ('BROUILLON', 'COMPLEMENT_UEP', 'COMPLEMENT_DIP') THEN
            RETURN 1;
        END IF;
        RETURN 0;
    END;

    MEMBER FUNCTION nb_documents RETURN NUMBER IS
        v_nb NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_nb FROM tb_ip_etude_document d WHERE d.id_etude = SELF.id_etude;
        RETURN v_nb;
    END;
END;
/

CREATE OR REPLACE TYPE BODY t_ip_avis AS
    MEMBER FUNCTION anomalie RETURN VARCHAR2 IS
        FUNCTION rempli (p CLOB) RETURN BOOLEAN IS
        BEGIN
            RETURN p IS NOT NULL AND DBMS_LOB.GETLENGTH(p) > 0;
        END;
    BEGIN
        IF NOT rempli(commentaire) THEN
            RETURN 'Rédigez l''avis de l''analyse.';
        END IF;
        IF decision IS NULL THEN
            RETURN 'Choisissez le résultat de l''analyse.';
        END IF;
        IF decision IN ('A_REVISER', 'REJETE') AND NOT rempli(recommandations) THEN
            RETURN 'Les recommandations sont obligatoires lorsque l''étude doit être révisée ou est rejetée.';
        END IF;
        RETURN NULL;
    END;
END;
/
