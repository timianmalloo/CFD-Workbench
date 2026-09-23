const fs = require('fs'), vm = require('vm');
const html=fs.readFileSync(process.argv[2],'utf8');
const script=html.split('<script>')[1].split('const $ =')[0];
const probe=`
const m=parseFoil(DEFAULT_DSL), g=deriveGeom(m);
const fine=DEFAULT_DSL.replace('x 14','x 14.049');
const a=parseFoil(fine), b=parseFoil(emitDSL(a));
const precision={before:a.leading.anchors[1].y,after:b.leading.anchors[1].y,hash:fnv1a(emitDSL(a)),baseHash:fnv1a(emitDSL(m))};
const nominal=parseFoil(DEFAULT_DSL.replace('naca 4412','file "missing.dat"')).sections.stations[0].spec;
const fractional=parseFoil(DEFAULT_DSL.replace('degree 3','degree 3.5')).loft.degree;
const duplicate=parseFoil(DEFAULT_DSL.replace('span  900 mm','span  800 mm span 900 mm')).span;
console.log(JSON.stringify({id:fnv1a(emitDSL(m)),area:g.S/100,aspect:g.AR,precision,nominal,fractional,duplicate},null,2));
`;
vm.runInNewContext(script+probe,{console});
