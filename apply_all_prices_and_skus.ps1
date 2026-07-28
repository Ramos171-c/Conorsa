$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$prices = @(
    # GALLETAS
    @{ Code='GA001'; SemiU=13.11; SemiC=393.26; MayU=12.96; MayC=388.89 },
    @{ Code='GA002'; SemiU=13.11; SemiC=393.26; MayU=12.96; MayC=388.89 },
    @{ Code='GA003'; SemiU=13.11; SemiC=393.26; MayU=12.96; MayC=388.89 },
    @{ Code='GA004'; SemiU=51.50; SemiC=1235.96; MayU=50.93; MayC=1222.22 },
    @{ Code='GA005'; SemiU=51.50; SemiC=1235.96; MayU=50.93; MayC=1222.22 },
    @{ Code='GA006'; SemiU=51.50; SemiC=1235.96; MayU=50.93; MayC=1222.22 },
    @{ Code='GA007'; SemiU=42.13; SemiC=1011.24; MayU=41.67; MayC=1000.00 },
    @{ Code='GA008'; SemiU=42.13; SemiC=1011.24; MayU=41.67; MayC=1000.00 },
    @{ Code='GA009'; SemiU=42.13; SemiC=1011.24; MayU=41.67; MayC=1000.00 },
    @{ Code='GA010'; SemiU=42.13; SemiC=1011.24; MayU=41.67; MayC=1000.00 },
    @{ Code='GA011'; SemiU=42.13; SemiC=1011.24; MayU=41.67; MayC=1000.00 },
    @{ Code='GA012'; SemiU=58.05; SemiC=1393.26; MayU=57.41; MayC=1377.78 },
    @{ Code='GA013'; SemiU=39.33; SemiC=943.82; MayU=38.89; MayC=933.33 },
    @{ Code='GA014'; SemiU=39.33; SemiC=943.82; MayU=38.89; MayC=933.33 },
    @{ Code='GA015'; SemiU=39.33; SemiC=943.82; MayU=38.89; MayC=933.33 },
    @{ Code='GA016'; SemiU=16.54; SemiC=595.51; MayU=16.36; MayC=588.89 },
    @{ Code='GA017'; SemiU=16.54; SemiC=595.51; MayU=16.36; MayC=588.89 },
    @{ Code='GA018'; SemiU=16.54; SemiC=595.51; MayU=16.36; MayC=588.89 },
    @{ Code='GA019'; SemiU=299.63; SemiC=1797.75; MayU=296.30; MayC=1777.78 },
    @{ Code='GA020'; SemiU=76.40; SemiC=764.04; MayU=75.56; MayC=755.56 },
    @{ Code='GA021'; SemiU=76.40; SemiC=764.04; MayU=75.56; MayC=755.56 },
    @{ Code='GA022'; SemiU=76.40; SemiC=764.04; MayU=75.56; MayC=755.56 },
    @{ Code='GA023'; SemiU=36.05; SemiC=865.17; MayU=35.65; MayC=855.56 },
    @{ Code='GA024'; SemiU=36.05; SemiC=865.17; MayU=35.65; MayC=855.56 },
    @{ Code='GA025'; SemiU=36.05; SemiC=865.17; MayU=35.65; MayC=855.56 },
    @{ Code='GA026'; SemiU=74.91; SemiC=449.44; MayU=74.07; MayC=444.44 },
    @{ Code='GA027'; SemiU=74.91; SemiC=449.44; MayU=74.07; MayC=444.44 },
    @{ Code='GA028'; SemiU=74.91; SemiC=449.44; MayU=74.07; MayC=444.44 },
    @{ Code='GA029'; SemiU=112.36; SemiC=224.72; MayU=111.11; MayC=222.22 },
    @{ Code='GA030'; SemiU=112.36; SemiC=224.72; MayU=111.11; MayC=222.22 },
    @{ Code='GA031'; SemiU=13.11; SemiC=393.26; MayU=12.96; MayC=388.89 },
    @{ Code='GA032'; SemiU=16.54; SemiC=595.51; MayU=16.36; MayC=588.89 },
    @{ Code='GA033'; SemiU=42.13; SemiC=1011.24; MayU=41.67; MayC=1000.00 },
    @{ Code='GA034'; SemiU=76.40; SemiC=764.04; MayU=75.56; MayC=755.56 },
    @{ Code='GA035'; SemiU=36.05; SemiC=865.17; MayU=35.65; MayC=855.56 },
    @{ Code='GA036'; SemiU=74.91; SemiC=449.44; MayU=74.07; MayC=444.44 },
    @{ Code='GA037'; SemiU=112.36; SemiC=224.72; MayU=111.11; MayC=222.22 },
    @{ Code='GA038'; SemiU=51.50; SemiC=1235.96; MayU=50.93; MayC=1222.22 },
    @{ Code='GA039'; SemiU=159.18; SemiC=1910.11; MayU=157.41; MayC=1888.89 },
    @{ Code='GA040'; SemiU=159.18; SemiC=1910.11; MayU=157.41; MayC=1888.89 },
    @{ Code='GA041'; SemiU=113.30; SemiC=2719.10; MayU=112.04; MayC=2688.89 },
    @{ Code='GA042'; SemiU=119.38; SemiC=2865.17; MayU=118.06; MayC=2833.33 },

    # CARAMELOS
    @{ Code='CA001'; SemiU=112.36; SemiC=1348.31; MayU=111.11; MayC=1333.33 },
    @{ Code='CA002'; SemiU=140.45; SemiC=1685.39; MayU=138.89; MayC=1666.67 },
    @{ Code='CA003'; SemiU=161.99; SemiC=1943.82; MayU=160.19; MayC=1922.22 },
    @{ Code='CA004'; SemiU=93.63; SemiC=2247.19; MayU=92.59; MayC=2222.22 },
    @{ Code='CA005'; SemiU=196.63; SemiC=2359.55; MayU=194.44; MayC=2333.33 },
    @{ Code='CA006'; SemiU=93.63; SemiC=1123.60; MayU=92.59; MayC=1111.11 },
    @{ Code='CA007'; SemiU=411.99; SemiC=2471.91; MayU=407.41; MayC=2444.44 },
    @{ Code='CA008'; SemiU=117.98; SemiC=2359.55; MayU=116.67; MayC=2333.33 },
    @{ Code='CA009'; SemiU=411.99; SemiC=2471.91; MayU=407.41; MayC=2444.44 },
    @{ Code='CA010'; SemiU=98.31; SemiC=786.52; MayU=97.22; MayC=777.78 },
    @{ Code='CA011'; SemiU=19.66; SemiC=786.52; MayU=19.44; MayC=777.78 },
    @{ Code='CA012'; SemiU=121.72; SemiC=1460.67; MayU=120.37; MayC=1444.44 },
    @{ Code='CA013'; SemiU=121.72; SemiC=1460.67; MayU=120.37; MayC=1444.44 },
    @{ Code='CA014'; SemiU=112.36; SemiC=1348.31; MayU=111.11; MayC=1333.33 },
    @{ Code='CA015'; SemiU=187.27; SemiC=2247.19; MayU=185.19; MayC=2222.22 },
    @{ Code='CA016'; SemiU=103.00; SemiC=2471.91; MayU=101.85; MayC=2444.44 },
    @{ Code='CA017'; SemiU=93.63; SemiC=2247.19; MayU=92.59; MayC=2222.22 },
    @{ Code='CA018'; SemiU=552.43; SemiC=8838.95; MayU=546.30; MayC=8740.74 },
    @{ Code='CA019'; SemiU=234.08; SemiC=2808.99; MayU=231.48; MayC=2777.78 },
    @{ Code='CA020'; SemiU=234.08; SemiC=2808.99; MayU=231.48; MayC=2777.78 },
    @{ Code='CA021'; SemiU=56.18; SemiC=1348.31; MayU=55.56; MayC=1333.33 },
    @{ Code='CA022'; SemiU=187.27; SemiC=1123.60; MayU=185.19; MayC=1111.11 },
    @{ Code='CA023'; SemiU=299.63; SemiC=1797.75; MayU=296.30; MayC=1777.78 },
    @{ Code='CA024'; SemiU=702.25; SemiC=2808.99; MayU=694.44; MayC=2777.78 },
    @{ Code='CA025'; SemiU=112.36; SemiC=2247.19; MayU=111.11; MayC=2222.22 },
    @{ Code='CA026'; SemiU=112.36; SemiC=2247.19; MayU=111.11; MayC=2222.22 },
    @{ Code='CA027'; SemiU=112.36; SemiC=2247.19; MayU=111.11; MayC=2222.22 },
    @{ Code='CA028'; SemiU=37.08; SemiC=3707.87; MayU=36.67; MayC=3666.67 },
    @{ Code='CA029'; SemiU=37.08; SemiC=3707.87; MayU=36.67; MayC=3666.67 },
    @{ Code='CA030'; SemiU=290.26; SemiC=3483.15; MayU=287.04; MayC=3444.44 },
    @{ Code='CA032'; SemiU=115.73; SemiC=2314.61; MayU=114.44; MayC=2288.89 },
    @{ Code='CA033'; SemiU=231.74; SemiC=1853.93; MayU=229.17; MayC=1833.33 },
    @{ Code='CA034'; SemiU=136.70; SemiC=1640.45; MayU=135.19; MayC=1622.22 },
    @{ Code='CA035'; SemiU=65.54; SemiC=1573.03; MayU=64.81; MayC=1555.56 },
    @{ Code='CA036'; SemiU=74.91; SemiC=898.88; MayU=74.07; MayC=888.89 },
    @{ Code='CA037'; SemiU=53.84; SemiC=1292.13; MayU=53.24; MayC=1277.78 },
    @{ Code='CA038'; SemiU=105.34; SemiC=842.70; MayU=104.17; MayC=833.33 },
    @{ Code='CA039'; SemiU=112.36; SemiC=1348.31; MayU=111.11; MayC=1333.33 },
    @{ Code='CA040'; SemiU=149.81; SemiC=1797.75; MayU=148.15; MayC=1777.78 },
    @{ Code='CA041'; SemiU=49.44; SemiC=1483.15; MayU=48.89; MayC=1466.67 },
    @{ Code='CA042'; SemiU=151.69; SemiC=1820.22; MayU=150.00; MayC=1800.00 },
    @{ Code='CA043'; SemiU=143.26; SemiC=2865.17; MayU=141.67; MayC=2833.33 },
    @{ Code='CA044'; SemiU=131.46; SemiC=2629.21; MayU=130.00; MayC=2600.00 },
    @{ Code='CA045'; SemiU=131.46; SemiC=2629.21; MayU=130.00; MayC=2600.00 },
    @{ Code='CA046'; SemiU=128.09; SemiC=2561.80; MayU=126.67; MayC=2533.33 },
    @{ Code='CA047'; SemiU=51.12; SemiC=1022.47; MayU=50.56; MayC=1011.11 },
    @{ Code='CA048'; SemiU=133.15; SemiC=2662.92; MayU=131.67; MayC=2633.33 },
    @{ Code='CA049'; SemiU=140.45; SemiC=2247.19; MayU=138.89; MayC=2222.22 },
    @{ Code='CA050'; SemiU=187.27; SemiC=2247.19; MayU=185.19; MayC=2222.22 },
    @{ Code='CA051'; SemiU=168.54; SemiC=1348.31; MayU=166.67; MayC=1333.33 },

    # MALVAVISCOS
    @{ Code='MA001'; SemiU=203.49; SemiC=1627.91; MayU=194.44; MayC=1555.56 },
    @{ Code='MA002'; SemiU=106.59; SemiC=1279.07; MayU=101.85; MayC=1222.22 },
    @{ Code='MA003'; SemiU=116.28; SemiC=1395.35; MayU=111.11; MayC=1333.33 },
    @{ Code='MA004'; SemiU=116.28; SemiC=1395.35; MayU=111.11; MayC=1333.33 },
    @{ Code='MA005'; SemiU=130.81; SemiC=1569.77; MayU=125.00; MayC=1500.00 },
    @{ Code='MA006'; SemiU=94.57; SemiC=1418.60; MayU=90.37; MayC=1355.56 },
    @{ Code='MA007'; SemiU=78.49; SemiC=1255.81; MayU=75.00; MayC=1200.00 },
    @{ Code='MA008'; SemiU=155.04; SemiC=1860.47; MayU=148.15; MayC=1777.78 },
    @{ Code='MA009'; SemiU=145.35; SemiC=2906.98; MayU=138.89; MayC=2777.78 },
    @{ Code='MA010'; SemiU=127.91; SemiC=2558.14; MayU=122.22; MayC=2444.44 },
    @{ Code='MA011'; SemiU=234.01; SemiC=1872.09; MayU=223.61; MayC=1788.89 },
    @{ Code='MA012'; SemiU=159.88; SemiC=1279.07; MayU=152.78; MayC=1222.22 },
    @{ Code='MA013'; SemiU=153.10; SemiC=3674.42; MayU=146.30; MayC=3511.11 },
    @{ Code='MA014'; SemiU=91.09; SemiC=2186.05; MayU=87.04; MayC=2088.89 },
    @{ Code='MA015'; SemiU=123.06; SemiC=2953.49; MayU=117.59; MayC=2822.22 },

    # TOALLAS Y OTROS
    @{ Code='TA001'; SemiU=56.82; SemiC=681.82; MayU=55.56; MayC=666.67 },
    @{ Code='TA002'; SemiU=61.55; SemiC=738.64; MayU=60.19; MayC=722.22 },
    @{ Code='TA003'; SemiU=64.39; SemiC=772.73; MayU=62.96; MayC=755.56 },
    @{ Code='TA004'; SemiU=58.71; SemiC=1761.36; MayU=57.41; MayC=1722.22 },
    @{ Code='TA005'; SemiU=42.61; SemiC=852.27; MayU=41.67; MayC=833.33 },
    @{ Code='TA006'; SemiU=21.31; SemiC=511.36; MayU=20.83; MayC=500.00 },
    @{ Code='TA007'; SemiU=553.98; SemiC=2215.91; MayU=541.67; MayC=2166.67 },
    @{ Code='TA008'; SemiU=553.98; SemiC=2215.91; MayU=541.67; MayC=2166.67 },
    @{ Code='TA009'; SemiU=255.68; SemiC=2045.45; MayU=250.00; MayC=2000.00 },
    @{ Code='TA010'; SemiU=56.82; SemiC=681.82; MayU=55.56; MayC=666.67 },
    @{ Code='TA011'; SemiU=553.98; SemiC=2215.91; MayU=541.67; MayC=2166.67 },
    @{ Code='TA012'; SemiU=284.09; SemiC=1136.36; MayU=277.78; MayC=1111.11 },
    @{ Code='TA013'; SemiU=21.31; SemiC=255.68; MayU=20.83; MayC=250.00 }
)

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

$updatedCount = 0

foreach ($p in $prices) {
    $code = $p.Code
    $semiU = $p.SemiU
    $semiC = $p.SemiC
    $mayU = $p.MayU
    $mayC = $p.MayC

    # Actualizar presentación base (IsBaseUnit = 1)
    $sqlBase = @"
UPDATE pp
SET pp.[SemiWholesalePrice] = $semiU,
    pp.[WholesalePrice] = $mayU
FROM ProductPresentations pp
JOIN Products prod ON pp.ProductId = prod.Id
WHERE prod.InternalCode = '$code' AND pp.IsBaseUnit = 1;
"@
    $cmdBase = $connection.CreateCommand()
    $cmdBase.CommandText = $sqlBase
    $updatedCount += $cmdBase.ExecuteNonQuery()

    # Actualizar presentación caja (IsBaseUnit = 0)
    $sqlBox = @"
UPDATE pp
SET pp.[SemiWholesalePrice] = $semiC,
    pp.[WholesalePrice] = $mayC
FROM ProductPresentations pp
JOIN Products prod ON pp.ProductId = prod.Id
WHERE prod.InternalCode = '$code' AND pp.IsBaseUnit = 0;
"@
    $cmdBox = $connection.CreateCommand()
    $cmdBox.CommandText = $sqlBox
    $updatedCount += $cmdBox.ExecuteNonQuery()
}

$connection.Close()

Write-Host "================ PRICES UPDATED ================"
Write-Host "Total ProductPresentations rows updated in DB: $updatedCount"
