const path = require('path');
const esbuild = require('esbuild');
const nodeGlobalsPlugin = require('@esbuild-plugins/node-globals-polyfill');
const nodeModulesPolyfill = require('@esbuild-plugins/node-modules-polyfill');

// Resolve paths dynamically to ensure they work no matter where the script is run from
const wrapperPath = path.resolve(__dirname, '../src/wrapper.js');
const outputPath = path.resolve(__dirname, '../build.js');

console.log("📂 Entry file:", wrapperPath);
console.log("📂 Output file:", outputPath);

esbuild.build({
  entryPoints: [wrapperPath],
  bundle: true,
  outfile: outputPath,
  format: 'esm',
  plugins: [nodeGlobalsPlugin.default(), nodeModulesPolyfill.default()],
}).catch(() => process.exit(1));