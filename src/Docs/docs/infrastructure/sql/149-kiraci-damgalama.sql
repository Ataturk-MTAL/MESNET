-- ADR-0003 adım 5 (#149) — mevcut satırları okulun kiracı kimliğiyle damgalar.
--
-- ÖNCE 149-conjoined-kiracilik.sql çalıştırılmış olmalıdır. O betik sütunları ekler ama
-- satırları '*DEFAULT*' kovasında bırakır; bu hâlde HİÇBİR OKUL KENDİ VERİSİNİ GÖREMEZ.
-- İki betiğin arası bir kesinti penceresidir, ayrı günlere bölünmez.
--
-- KULLANIM — hedef kiracıyı aşağıdaki tek satırda ayarlayın:
--   psql -d mesnet -v ON_ERROR_STOP=1 \
--        -v tenant="$(psql -tAc "select id from institution.mt_doc_institution")" \
--        -f 149-kiraci-damgalama.sql
--
-- Değer verilmezse betik durur — yanlış kiracıya damga atmaktansa hiç atmamak iyidir.
--
-- N=1 iken bu tek bir toplu UPDATE'tir. İkinci okul veri yazdıktan sonra aynı işi yapmak
-- adli inceleme olur: hangi satırın hangi okula ait olduğu artık kolonlardan okunamaz.

\if :{?tenant}
\else
  \echo 'HATA: hedef kiracı verilmedi. -v tenant=<institution-id> ile çalıştırın.'
  \quit
\endif

BEGIN;

-- psql değişkenini oturum ayarına köprüle: DO bloğu plpgsql'dir, psql değişkenini göremez.
SET LOCAL mesnet.hedef_kiraci = :'tenant';

-- Olay tabloları: mt_events → mt_streams yabancı anahtarı (tenant_id, stream_id) üzerindedir.
-- İkisi birlikte damgalanmalı; kısıt açıkken ara adımda çocuk satırlar öksüz kalır ve
-- bütün göç geri alınır. Gerçekten yaşandı: 4106 satırlık UPDATE bu yüzden geri alınmıştı.
ALTER TABLE shared.mt_events DROP CONSTRAINT IF EXISTS fkey_mt_events_stream_id_tenant_id;

DO $$
DECLARE
    r record;
    hedef text := current_setting('mesnet.hedef_kiraci');
    guncellenen bigint;
    toplam bigint := 0;
BEGIN
    -- Damga YALNIZ tenant_id sütunu olan tablolara atılır. O sütunun varlığı, belgenin
    -- DocumentTenancyMap'te "Tenant" sınıflandırıldığının doğrudan sonucudur — paylaşımlı
    -- ve kimlik katmanı belgeleri sütunu hiç taşımaz, dolayısıyla bu döngüye giremez.
    FOR r IN
        SELECT table_schema AS s, table_name AS t
        FROM information_schema.columns
        WHERE column_name = 'tenant_id'
          AND table_schema NOT IN ('pg_catalog', 'information_schema')
        ORDER BY 1, 2
    LOOP
        EXECUTE format(
            'UPDATE %I.%I SET tenant_id = %L WHERE tenant_id = ''*DEFAULT*''',
            r.s, r.t, hedef);
        GET DIAGNOSTICS guncellenen = ROW_COUNT;
        toplam := toplam + guncellenen;
        IF guncellenen > 0 THEN
            RAISE NOTICE '% .% -> % satir', r.s, r.t, guncellenen;
        END IF;
    END LOOP;
    RAISE NOTICE 'TOPLAM: % satir damgalandi', toplam;
END $$;

ALTER TABLE shared.mt_events
ADD CONSTRAINT fkey_mt_events_stream_id_tenant_id FOREIGN KEY (tenant_id, stream_id)
REFERENCES shared.mt_streams (tenant_id, id);

COMMIT;

-- Doğrulama: sıfır dönmelidir. Sıfır değilse damgalama eksiktir ve o satırlar hiçbir okulun
-- sorgusunda görünmez. Aynı kontrol CI'da kalıcıdır: TenantStampIntegrityTests.
DO $$
DECLARE r record; n bigint; toplam bigint := 0;
BEGIN
  FOR r IN SELECT table_schema s, table_name t FROM information_schema.columns
           WHERE column_name = 'tenant_id'
             AND table_schema NOT IN ('pg_catalog','information_schema') LOOP
    EXECUTE format('SELECT count(*) FROM %I.%I WHERE tenant_id = ''*DEFAULT*''', r.s, r.t) INTO n;
    toplam := toplam + n;
    IF n > 0 THEN RAISE WARNING 'DAMGASIZ KALDI %.% -> %', r.s, r.t, n; END IF;
  END LOOP;
  RAISE NOTICE 'DAMGASIZ TOPLAM: % (0 olmali)', toplam;
END $$;
