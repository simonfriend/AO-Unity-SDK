const esbuild = require('esbuild');
const nodeGlobalsPlugin = require('@esbuild-plugins/node-globals-polyfill');
const nodeModulesPolyfill = require('@esbuild-plugins/node-modules-polyfill');

esbuild.build({
  entryPoints: ['src/wrapper.js'],
  bundle: true,
  outfile: 'build.js', 
  format: 'esm',
  plugins: [nodeGlobalsPlugin.default(), nodeModulesPolyfill.default()],
}).catch(() => process.exit(1));