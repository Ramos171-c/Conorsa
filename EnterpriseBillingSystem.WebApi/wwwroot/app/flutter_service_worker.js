'use strict';
const MANIFEST = 'flutter-app-manifest';
const TEMP = 'flutter-temp-cache';
const CACHE_NAME = 'flutter-app-cache';

const RESOURCES = {"assets/AssetManifest.bin": "903c8c8ca8747359c92f0149ff40bb66",
"assets/AssetManifest.bin.json": "b424ba0d73ad93847559681918e43580",
"assets/AssetManifest.json": "a87af06b233b6fdcd189131b179da0eb",
"assets/assets/images/CA001.jpeg": "50bde661951095135a2986ee38348c07",
"assets/assets/images/CA002.png": "e42c4f3386cdbd3af1a4e8f50ef325b2",
"assets/assets/images/CA003.png": "95b3031af868a0a59dd8ec11ad65ccb1",
"assets/assets/images/CA004.png": "295c9c3605988fc30514e93c2eb7573c",
"assets/assets/images/CA005.png": "f283f9782b2b7c05f23a339c8f934afa",
"assets/assets/images/CA006.png": "5da8274a80c19b28742e1a7b3d2a0529",
"assets/assets/images/CA007.png": "8162d87a8e046f4df7e052c0a6bf991f",
"assets/assets/images/CA008.jpeg": "4cafc69b9d2539db597bb61f5589d33c",
"assets/assets/images/CA009.jpeg": "a337d8b0627add660f395ccd39fb8804",
"assets/assets/images/CA010.png": "5402d8d50333507710ffa72834e06862",
"assets/assets/images/CA011.jpeg": "59e2f941b673622c2d85af1aa872ca5f",
"assets/assets/images/CA012.jpeg": "8fa076e3157c0f24d8e353926480060a",
"assets/assets/images/CA013.png": "ce79138ed8575e3e30081cee7b2f8aa1",
"assets/assets/images/CA014.png": "f2e06264c7aa08818c29d949af59bab2",
"assets/assets/images/CA015.jpg": "279d68018d6b597484df533f6e657340",
"assets/assets/images/CA016.png": "286bd6ef03a66222d91eb00646cf34c5",
"assets/assets/images/CA017.png": "1fb66287897b98aeabe570fe382e5e85",
"assets/assets/images/CA019.png": "1604841460ce7671a111639007697d9d",
"assets/assets/images/CA020.png": "a2f1efe71531bf925f562bf38df1ded2",
"assets/assets/images/CA021.png": "9acf045a91614d12db29210816234a94",
"assets/assets/images/CA022.jpeg": "62c0ee1b1c006815fc91cb017a60ca5f",
"assets/assets/images/CA023.jpeg": "11a1fe8a0118cc24c3dbb8191a63303b",
"assets/assets/images/CA024.jpeg": "39ee7038ef7895a1832a944599a6ad61",
"assets/assets/images/CA025.png": "713ec368b17c6f9d6191f873eaba5026",
"assets/assets/images/CA026.png": "12d4850e7df452b4faa854edd09bb229",
"assets/assets/images/CA027.png": "7a842175cb17e2e9df59a99d7ac09707",
"assets/assets/images/CA028.png": "30b26dea491afd6a4ffce015939bab9d",
"assets/assets/images/CA029.png": "19a979dba0751c10608782c53accc9ea",
"assets/assets/images/CA030.png": "a81464faa3d658f044a60f5b3ae82176",
"assets/assets/images/CA031.png": "5103dc49cc23bf01c8d6435e67db1a76",
"assets/assets/images/CA032.png": "7fa64af00abbff5586715c3f9d44ca97",
"assets/assets/images/CA033.png": "970bb0aee49b8d149ac39ee77602a3cf",
"assets/assets/images/CA034.png": "3df77a8e3140dfd0803103d681853662",
"assets/assets/images/CA035.jpeg": "7cf3251cf5b9b2b701b239a819247cea",
"assets/assets/images/CA036.jpeg": "3d34b34edffcaa4ae80d15ef7357edb7",
"assets/assets/images/CA037.jpeg": "d78efac679fea1d0bfe185ded040addb",
"assets/assets/images/CA038.jpeg": "5526786d43c8d0b6170fdda780870ab1",
"assets/assets/images/CA039.png": "7283889fd85f69b01f4f677853f62268",
"assets/assets/images/CA040.png": "6fa009ea8be41d3213ef2f87d03aa4b3",
"assets/assets/images/CA041.png": "41ccf2f9eaceb49e02106ecdb5d39b56",
"assets/assets/images/CA042.png": "df2e6ba590abf226401ee4e5e71b98eb",
"assets/assets/images/GA001.png": "97af19b156c097529dee1cad46ac539e",
"assets/assets/images/GA002.png": "712b8e287a58c38f60ac420871228b85",
"assets/assets/images/GA003.png": "ebc66a2defe8586d04ca1ea0b428b042",
"assets/assets/images/GA004.png": "08215742d62fb27acc655da1ab824376",
"assets/assets/images/GA005.png": "3cf8c56faac2a16219eba2abe1cbd05b",
"assets/assets/images/GA006.png": "cd362917b8793219f8c7cbcd48e0fd53",
"assets/assets/images/GA007.jpeg": "290c899e052d3cb8321cd854fcf6e572",
"assets/assets/images/GA008.jpeg": "39438ed18a830013a790fc16a4c574e8",
"assets/assets/images/GA009.jpeg": "9e1f25546bc013d6ceef19fa6d5afcd1",
"assets/assets/images/GA010.jpeg": "a130c1ad1e4b3d01d4cd092bef4fe1ad",
"assets/assets/images/GA011.jpeg": "9ef57890740c7a539be912c2a25e39d7",
"assets/assets/images/GA012.png": "4667c6c4ec55ed1c303471e79b54769c",
"assets/assets/images/GA013.jpeg": "2b0ba4298e807585d43e7f1edc1a204c",
"assets/assets/images/GA014.jpeg": "b9b61d07651b40942505ff47ee6fc668",
"assets/assets/images/GA015.jpeg": "d76f21d53ef1420eddbaf7c4db9d5f77",
"assets/assets/images/GA016.png": "fe3c3ca10c8d8c3a2e6bfc5284783a21",
"assets/assets/images/GA017.png": "e2b7123bd5b57942aac9eb93badcf420",
"assets/assets/images/GA018.png": "d3af47668ef0d0504c7781728d9b6ac6",
"assets/assets/images/GA020.jpeg": "6ad74a7665d0868490444c6bb14a5110",
"assets/assets/images/GA021.jpeg": "7a5b54111d501d3cfe70d58fa7879ff5",
"assets/assets/images/GA022.jpeg": "892d4043902cc23d2b352d297b299520",
"assets/assets/images/GA023.png": "b8d844646a1d1387018073a9c1bf9c0d",
"assets/assets/images/GA024.png": "a8202cf7befc714af5f90ce151b87d0e",
"assets/assets/images/GA025.jpeg": "4bbd109c91d5e07ad2932e04f79927e8",
"assets/assets/images/MA001.png": "100d0d1343426e0c2b2ca19bb0dc0fec",
"assets/assets/images/MA002.jpeg": "8b3dd161d0b3bd28109dfd8c2c3358ac",
"assets/assets/images/MA003.jpeg": "a8757812e44ee338ad15c1431dd2c264",
"assets/assets/images/MA004.jpeg": "5abf12063bdcbe26869920aad1dbde14",
"assets/assets/images/MA005.png": "ae5d6aed0a897524b0ab022788d66113",
"assets/assets/images/MA006.png": "0c26bc0dab2d23fbd6c38168d33426e8",
"assets/assets/images/MA007.png": "5875815ab5fabd6fbb4edeb3db30fb6b",
"assets/assets/images/MA008.png": "a7a3b6394f9728483e7f512760ae930c",
"assets/assets/images/MA009.png": "3500d03f33ae574b604a881309990540",
"assets/assets/images/MA010.jpg": "fc2dcdd5a2c2109e42b5925da73b3154",
"assets/assets/images/MA011.png": "4ceb6fb1cc0fc980c8476e9fe4d69798",
"assets/assets/images/MA012.png": "357f0778ecce3083cdd67b7ff2032c48",
"assets/assets/images/TA001.png": "0bba1e742ef953ee28e545df3d6abbeb",
"assets/assets/images/TA002.jpeg": "e9f6008e897eb8f6343daefcc952a29b",
"assets/assets/images/TA003.jpeg": "6cd999e633c75642304103f9a14b01be",
"assets/assets/images/TA004.png": "9b2ba8ffa9b8ce27c8848022603ba9a3",
"assets/assets/images/TA005.png": "3e090da54cf282b2e964ea8bd3f38e2d",
"assets/assets/images/TA006.png": "92170a54959023de416731e993316806",
"assets/assets/images/TA007.png": "d543fb99dc879acab66567a13cac7d61",
"assets/assets/images/TA008.jpeg": "e3da790ca2a9e4d78aaf38e5a4c71635",
"assets/assets/images/TA009.png": "ace00540ebb2c29641a6301777a6da40",
"assets/assets/images/TA010.jpeg": "0f5c219b203f412c5ba0aa6991db5126",
"assets/assets/images/TO011.jpeg": "eeef700bd3d142eb98b04fba691964c8",
"assets/assets/images/TO012.png": "eb0ce0aa39d1ff073690118f68c7a9c7",
"assets/FontManifest.json": "dc3d03800ccca4601324923c0b1d6d57",
"assets/fonts/MaterialIcons-Regular.otf": "288160d1f06cdc0ed912443412d8c3aa",
"assets/NOTICES": "e6f806147389af2c62c1c500390c2640",
"assets/packages/cupertino_icons/assets/CupertinoIcons.ttf": "33b7d9392238c04c131b6ce224e13711",
"assets/shaders/ink_sparkle.frag": "ecc85a2e95f5e9f53123dcaf8cb9b6ce",
"canvaskit/canvaskit.js": "728b2d477d9b8c14593d4f9b82b484f3",
"canvaskit/canvaskit.js.symbols": "bdcd3835edf8586b6d6edfce8749fb77",
"canvaskit/canvaskit.wasm": "7a3f4ae7d65fc1de6a6e7ddd3224bc93",
"canvaskit/chromium/canvaskit.js": "8191e843020c832c9cf8852a4b909d4c",
"canvaskit/chromium/canvaskit.js.symbols": "b61b5f4673c9698029fa0a746a9ad581",
"canvaskit/chromium/canvaskit.wasm": "f504de372e31c8031018a9ec0a9ef5f0",
"canvaskit/skwasm.js": "ea559890a088fe28b4ddf70e17e60052",
"canvaskit/skwasm.js.symbols": "e72c79950c8a8483d826a7f0560573a1",
"canvaskit/skwasm.wasm": "39dd80367a4e71582d234948adc521c0",
"favicon.png": "5dcef449791fa27946b3d35ad8803796",
"flutter.js": "83d881c1dbb6d6bcd6b42e274605b69c",
"flutter_bootstrap.js": "ca49a53a760a793f459781913c2bbd19",
"icons/Icon-192.png": "ac9a721a12bbc803b44f645561ecb1e1",
"icons/Icon-512.png": "96e752610906ba2a93c65f8abe1645f1",
"icons/Icon-maskable-192.png": "c457ef57daa1d16f64b27b786ec2ea3c",
"icons/Icon-maskable-512.png": "301a7604d45b3e739efc881eb04896ea",
"index.html": "5cc0fad595073a1b32fe46f6faebab56",
"/": "5cc0fad595073a1b32fe46f6faebab56",
"main.dart.js": "e4216e8db8bc7d73720bff1c69cf19eb",
"manifest.json": "834a37c718a1d9b7e2a4811acf26bf8d",
"version.json": "a7e55a991cc69f60ca0a7cbd4169ae40"};
// The application shell files that are downloaded before a service worker can
// start.
const CORE = ["main.dart.js",
"index.html",
"flutter_bootstrap.js",
"assets/AssetManifest.bin.json",
"assets/FontManifest.json"];

// During install, the TEMP cache is populated with the application shell files.
self.addEventListener("install", (event) => {
  self.skipWaiting();
  return event.waitUntil(
    caches.open(TEMP).then((cache) => {
      return cache.addAll(
        CORE.map((value) => new Request(value, {'cache': 'reload'})));
    })
  );
});
// During activate, the cache is populated with the temp files downloaded in
// install. If this service worker is upgrading from one with a saved
// MANIFEST, then use this to retain unchanged resource files.
self.addEventListener("activate", function(event) {
  return event.waitUntil(async function() {
    try {
      var contentCache = await caches.open(CACHE_NAME);
      var tempCache = await caches.open(TEMP);
      var manifestCache = await caches.open(MANIFEST);
      var manifest = await manifestCache.match('manifest');
      // When there is no prior manifest, clear the entire cache.
      if (!manifest) {
        await caches.delete(CACHE_NAME);
        contentCache = await caches.open(CACHE_NAME);
        for (var request of await tempCache.keys()) {
          var response = await tempCache.match(request);
          await contentCache.put(request, response);
        }
        await caches.delete(TEMP);
        // Save the manifest to make future upgrades efficient.
        await manifestCache.put('manifest', new Response(JSON.stringify(RESOURCES)));
        // Claim client to enable caching on first launch
        self.clients.claim();
        return;
      }
      var oldManifest = await manifest.json();
      var origin = self.location.origin;
      for (var request of await contentCache.keys()) {
        var key = request.url.substring(origin.length + 1);
        if (key == "") {
          key = "/";
        }
        // If a resource from the old manifest is not in the new cache, or if
        // the MD5 sum has changed, delete it. Otherwise the resource is left
        // in the cache and can be reused by the new service worker.
        if (!RESOURCES[key] || RESOURCES[key] != oldManifest[key]) {
          await contentCache.delete(request);
        }
      }
      // Populate the cache with the app shell TEMP files, potentially overwriting
      // cache files preserved above.
      for (var request of await tempCache.keys()) {
        var response = await tempCache.match(request);
        await contentCache.put(request, response);
      }
      await caches.delete(TEMP);
      // Save the manifest to make future upgrades efficient.
      await manifestCache.put('manifest', new Response(JSON.stringify(RESOURCES)));
      // Claim client to enable caching on first launch
      self.clients.claim();
      return;
    } catch (err) {
      // On an unhandled exception the state of the cache cannot be guaranteed.
      console.error('Failed to upgrade service worker: ' + err);
      await caches.delete(CACHE_NAME);
      await caches.delete(TEMP);
      await caches.delete(MANIFEST);
    }
  }());
});
// The fetch handler redirects requests for RESOURCE files to the service
// worker cache.
self.addEventListener("fetch", (event) => {
  if (event.request.method !== 'GET') {
    return;
  }
  var origin = self.location.origin;
  var key = event.request.url.substring(origin.length + 1);
  // Redirect URLs to the index.html
  if (key.indexOf('?v=') != -1) {
    key = key.split('?v=')[0];
  }
  if (event.request.url == origin || event.request.url.startsWith(origin + '/#') || key == '') {
    key = '/';
  }
  // If the URL is not the RESOURCE list then return to signal that the
  // browser should take over.
  if (!RESOURCES[key]) {
    return;
  }
  // If the URL is the index.html, perform an online-first request.
  if (key == '/') {
    return onlineFirst(event);
  }
  event.respondWith(caches.open(CACHE_NAME)
    .then((cache) =>  {
      return cache.match(event.request).then((response) => {
        // Either respond with the cached resource, or perform a fetch and
        // lazily populate the cache only if the resource was successfully fetched.
        return response || fetch(event.request).then((response) => {
          if (response && Boolean(response.ok)) {
            cache.put(event.request, response.clone());
          }
          return response;
        });
      })
    })
  );
});
self.addEventListener('message', (event) => {
  // SkipWaiting can be used to immediately activate a waiting service worker.
  // This will also require a page refresh triggered by the main worker.
  if (event.data === 'skipWaiting') {
    self.skipWaiting();
    return;
  }
  if (event.data === 'downloadOffline') {
    downloadOffline();
    return;
  }
});
// Download offline will check the RESOURCES for all files not in the cache
// and populate them.
async function downloadOffline() {
  var resources = [];
  var contentCache = await caches.open(CACHE_NAME);
  var currentContent = {};
  for (var request of await contentCache.keys()) {
    var key = request.url.substring(origin.length + 1);
    if (key == "") {
      key = "/";
    }
    currentContent[key] = true;
  }
  for (var resourceKey of Object.keys(RESOURCES)) {
    if (!currentContent[resourceKey]) {
      resources.push(resourceKey);
    }
  }
  return contentCache.addAll(resources);
}
// Attempt to download the resource online before falling back to
// the offline cache.
function onlineFirst(event) {
  return event.respondWith(
    fetch(event.request).then((response) => {
      return caches.open(CACHE_NAME).then((cache) => {
        cache.put(event.request, response.clone());
        return response;
      });
    }).catch((error) => {
      return caches.open(CACHE_NAME).then((cache) => {
        return cache.match(event.request).then((response) => {
          if (response != null) {
            return response;
          }
          throw error;
        });
      });
    })
  );
}
