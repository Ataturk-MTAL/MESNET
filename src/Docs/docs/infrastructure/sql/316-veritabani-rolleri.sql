-- ════════════════════════════════════════════════════════════════════════════════════════
-- #316 — Veritabanı rolleri: API süper kullanıcıyla bağlanmaz
-- ════════════════════════════════════════════════════════════════════════════════════════
--
-- NEDEN: süper kullanıcı row-level security'yi HER ZAMAN atlar (FORCE bile işlemez). API
-- süper kullanıcıyla bağlandıkça veritabanı katmanındaki her kiracı yalıtımı yerinde durur
-- ama hiçbir şeyi süzmez — ve bunu hiçbir test göstermez.
--
--   mesnet_owner  şemanın sahibi, DDL. Yalnız göç adımı (resources setup) ve dev'de API.
--   mesnet_app    yalnız DML. Üretimde ve CI'da API çalışma zamanı.
--   İkisi de NOSUPERUSER NOBYPASSRLS — açılış kontrolü (DatabaseRoleVerification) bunu doğrular.
--
-- KİM ÇALIŞTIRIR: süper kullanıcı, hedef veritabanında. İDEMPOTENT — her dağıtımda,
-- dev'de her açılışta çalışır (db-init adımı). Yeni veritabanında ve var olan veritabanında
-- aynı betik: ikinci bölüm var olan nesnelerin sahipliğini devreder, yeni kurulumda boş geçer.
--
--   psql -v ON_ERROR_STOP=1 -d mesnet \
--        -v owner_password="$MESNET_OWNER_PASSWORD" -v app_password="$MESNET_APP_PASSWORD" \
--        -f 316-veritabani-rolleri.sql
--
-- Sıra ve gerekçe: src/Docs/docs/infrastructure/dagitim-on-kosullari.md
-- ════════════════════════════════════════════════════════════════════════════════════════

\set ON_ERROR_STOP on

-- Ön koşullar RAISE ile denetlenir, \quit ile DEĞİL: psql'in \quit'i çıkış kodu almaz ve
-- reddi BAŞARI koduyla bitirir (ölçüldü) — göç zinciri bunu "tamam" sanıp devam ederdi.
-- ON_ERROR_STOP altında RAISE, psql'i 3 koduyla durdurur.
DO $$
BEGIN
    IF current_setting('is_superuser') <> 'on' THEN
        RAISE EXCEPTION '316: bu betik süper kullanıcıyla çalıştırılmalı (rol ve eklenti kurar), bağlanan: %', current_user;
    END IF;
END
$$;

\if :{?owner_password}
\else
  DO $$ BEGIN RAISE EXCEPTION '316: -v owner_password=... verilmedi'; END $$;
\endif
\if :{?app_password}
\else
  DO $$ BEGIN RAISE EXCEPTION '316: -v app_password=... verilmedi'; END $$;
\endif

SELECT current_database() AS db \gset

-- ── 1. Roller ─────────────────────────────────────────────────────────────────────────────
-- Özellikler her koşuda yeniden YAZILIR: elle verilmiş bir SUPERUSER/BYPASSRLS bir sonraki
-- dağıtımda geri alınır.
SELECT NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'mesnet_owner') AS create_owner \gset
\if :create_owner
  CREATE ROLE mesnet_owner;
\endif
ALTER ROLE mesnet_owner LOGIN NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE NOREPLICATION
    PASSWORD :'owner_password';

SELECT NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'mesnet_app') AS create_app \gset
\if :create_app
  CREATE ROLE mesnet_app;
\endif
-- NOINHERIT: ileride mesnet_app'e bir rol üyeliği verilirse yetkiler kendiliğinden geçmesin.
ALTER ROLE mesnet_app LOGIN NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE NOREPLICATION NOINHERIT
    PASSWORD :'app_password';

-- ── 2. Veritabanı ─────────────────────────────────────────────────────────────────────────
-- Sahiplik DEVREDİLMEZ, yalnız CREATE (şema açma) verilir: veritabanı sahibi public şemasının
-- da sahibi olur (PG15+ pg_database_owner) ve public'te Keycloak tabloları ile PostGIS durur.
GRANT CONNECT, CREATE ON DATABASE :"db" TO mesnet_owner;
GRANT CONNECT ON DATABASE :"db" TO mesnet_app;

-- PostGIS güvenilir (trusted) eklenti DEĞİLDİR: yalnız süper kullanıcı kurar. Eskiden API
-- açılışta kuruyordu — bu, API'nin süper kullanıcı olmasını gerektiriyordu.
CREATE EXTENSION IF NOT EXISTS postgis;

-- ── 3. Bundan sonra kurulacak nesneler ────────────────────────────────────────────────────
-- Göç adımı (mesnet_owner) yeni bir şema/tablo açtığında mesnet_app'in yetkisi
-- kendiliğinden gelir. Yoksa her yeni belge tipi üretimde "permission denied" ile doğardı.
ALTER DEFAULT PRIVILEGES FOR ROLE mesnet_owner GRANT USAGE ON SCHEMAS TO mesnet_app;
ALTER DEFAULT PRIVILEGES FOR ROLE mesnet_owner
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO mesnet_app;
ALTER DEFAULT PRIVILEGES FOR ROLE mesnet_owner GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO mesnet_app;
ALTER DEFAULT PRIVILEGES FOR ROLE mesnet_owner GRANT EXECUTE ON FUNCTIONS TO mesnet_app;

-- ── 4. Var olan nesneler (yükseltme) ──────────────────────────────────────────────────────
-- Önceki sürümlerde şemayı API süper kullanıcıyla kurdu; nesnelerin sahibi o. Göç adımı
-- (mesnet_owner) onları ALTER edebilsin diye sahiplik devredilir, mesnet_app'e yetki verilir.
-- Kapsam POZİTİF tanımlanır: yalnız MESNET'in şemaları — Marten (mt_*) ya da Wolverine
-- (wolverine*) nesnesi taşıyanlar. "public dışındaki her şema" DEĞİL: üretimde Keycloak aynı
-- veritabanında `keycloak` şemasını kullanır ve o kural mesnet_app'e Keycloak tablolarında
-- (parola özetleri dahil) yazma yetkisi verirdi. `keycloak` ayrıca adıyla da dışarıda tutulur.
-- Yeni kurulumda döngü boş geçer.
DO $$
DECLARE
    s   record;
    obj record;
BEGIN
    FOR s IN
        SELECT n.nspname
        FROM pg_namespace n
        WHERE n.nspname NOT IN ('public', 'information_schema', 'keycloak')
          AND n.nspname !~ '^pg_'
          AND NOT EXISTS (SELECT 1 FROM pg_depend d
                          WHERE d.classid = 'pg_namespace'::regclass
                            AND d.objid = n.oid AND d.deptype = 'e')
          AND (EXISTS (SELECT 1 FROM pg_class c WHERE c.relnamespace = n.oid
                         AND (c.relname LIKE 'mt\_%' OR c.relname LIKE 'wolverine%'))
               OR EXISTS (SELECT 1 FROM pg_proc p WHERE p.pronamespace = n.oid
                         AND p.proname LIKE 'mt\_%'))
    LOOP
        EXECUTE format('ALTER SCHEMA %I OWNER TO mesnet_owner', s.nspname);

        -- Tablolar, görünümler, sekanslar. Sütuna bağlı (serial/identity) sekanslar tabloyla
        -- birlikte devrolur; ayrıca ALTER edilirse hata verir.
        FOR obj IN
            SELECT c.relname, c.relkind
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = s.nspname
              AND c.relkind IN ('r', 'p', 'v', 'm', 'S', 'f')
              AND pg_get_userbyid(c.relowner) <> 'mesnet_owner'
              AND NOT (c.relkind = 'S' AND EXISTS (
                    SELECT 1 FROM pg_depend d
                    WHERE d.classid = 'pg_class'::regclass AND d.objid = c.oid
                      AND d.deptype IN ('a', 'i')))
        LOOP
            EXECUTE format('ALTER %s %I.%I OWNER TO mesnet_owner',
                CASE obj.relkind
                    WHEN 'v' THEN 'VIEW'
                    WHEN 'm' THEN 'MATERIALIZED VIEW'
                    WHEN 'S' THEN 'SEQUENCE'
                    WHEN 'f' THEN 'FOREIGN TABLE'
                    ELSE 'TABLE'
                END,
                s.nspname, obj.relname);
        END LOOP;

        -- Fonksiyonlar (Marten mt_upsert_* vb.)
        FOR obj IN
            SELECT p.oid::regprocedure::text AS sig
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            WHERE n.nspname = s.nspname
              AND pg_get_userbyid(p.proowner) <> 'mesnet_owner'
        LOOP
            EXECUTE format('ALTER ROUTINE %s OWNER TO mesnet_owner', obj.sig);
        END LOOP;

        -- Tipler (bileşik tipler tabloyla gelir, onları atla)
        FOR obj IN
            SELECT format('%I.%I', n.nspname, t.typname) AS name
            FROM pg_type t
            JOIN pg_namespace n ON n.oid = t.typnamespace
            WHERE n.nspname = s.nspname
              AND t.typtype IN ('e', 'd', 'c')
              AND (t.typrelid = 0 OR EXISTS (SELECT 1 FROM pg_class c
                                             WHERE c.oid = t.typrelid AND c.relkind = 'c'))
              AND pg_get_userbyid(t.typowner) <> 'mesnet_owner'
        LOOP
            EXECUTE format('ALTER TYPE %s OWNER TO mesnet_owner', obj.name);
        END LOOP;

        EXECUTE format('GRANT USAGE ON SCHEMA %I TO mesnet_app', s.nspname);
        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO mesnet_app', s.nspname);
        EXECUTE format('GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA %I TO mesnet_app', s.nspname);
        EXECUTE format('GRANT EXECUTE ON ALL ROUTINES IN SCHEMA %I TO mesnet_app', s.nspname);
    END LOOP;
END
$$;

\echo '316-veritabani-rolleri: tamam'
