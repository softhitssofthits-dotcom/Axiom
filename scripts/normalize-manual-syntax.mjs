import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
const names={array:'массив',buffer:'буфер',text:'текст',pattern:'шаблон',modes:'режимы',count:'количество',start:'начальная позиция',position:'позиция',index:'индекс',dimension:'измерение',file:'файл',path:'путь',source:'источник',destination:'назначение',value:'значение',width:'ширина',height:'высота',unit:'единица измерения',code:'код',group:'номер группы',groupNumber:'номер группы',groupIndex:'номер группы',replacement:'замена',separator:'разделитель',year:'год',month:'месяц',day:'день',hour:'час',minute:'минута',second:'секунда',timeZone:'часовой пояс',interval:'интервал',number:'число',predicate:'функция проверки',selector:'функция преобразования',reducer:'функция накопления',seed:'начальное значение',template:'образец',image:'изображение',sprite:'спрайт',tileset:'набор тайлов',tilemap:'тайловая карта',encoding:'кодировка',overwrite:'разрешение перезаписи',fill:'заполнение',fragment:'фрагмент',keyText:'клавиша',buttonNumber:'номер кнопки',layerIndex:'индекс слоя',objectIndex:'индекс объекта',coordinate:'координата',column:'столбец',row:'строка',fromValue:'начальное значение',toValue:'конечное значение',red:'красный',green:'зелёный',blue:'синий',frameWidth:'ширина кадра',frameHeight:'высота кадра',tileWidth:'ширина тайла',tileHeight:'высота тайла',margin:'отступ',spacing:'промежуток',worldX:'мировая x',worldY:'мировая y',originX:'начальная x',originY:'начальная y',left:'левый операнд',right:'правый операнд',low:'нижняя граница',high:'верхняя граница',power:'показатель степени',prefix:'префикс',weekday:'день недели',name:'имя',newName:'новое имя',chars:'символы',items:'элементы',newItems:'новые элементы',oldItems:'исходные элементы',newText:'новый текст',oldText:'исходный текст',part:'часть',signFrom:'источник знака',result:'результат'};
export const formalParameterName=name=>names[name]||name;
const decode=s=>s.replaceAll('&lt;','<').replaceAll('&gt;','>').replaceAll('&amp;','&').replaceAll('&quot;','"');
const escape=s=>s.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');
// ⟦…⟧ — метанотация необязательных частей; [] — буквальный синтаксис массива.
// Старые схемы переводятся только по признакам метанотации, не по всем скобкам.
export function normalizeOptionalNotation(source){
 let result='',quoted=false;const brackets=[];
 for(let i=0;i<source.length;i++){
  const char=source[i];
  if(char==='"'){
   result+=char;
   if(quoted&&source[i+1]==='"'){result+=source[++i];continue;}
   quoted=!quoted;continue;
  }
  if(!quoted&&char==='['){
   const tail=source.slice(i+1);
   const attached=/[A-Za-zА-Яа-яЁё0-9_>\])]$/.test(source.slice(0,i));
   const optional=/^[,;]\s*(?:<|[A-Za-zА-Яа-яЁё]|\.\.\.|\|)/.test(tail)||!attached&&/^(?:<|STEP\b|ELSEIF\b|ELSE\b|CASE\b)/.test(tail);
   brackets.push(optional);result+=optional?'⟦':'[';
  }else if(!quoted&&char===']')result+=brackets.pop()?'⟧':']';
  else result+=char;
 }
 return result;
}
function formal(source,dictionary){
 const known=new Set([...dictionary.keywords,...dictionary.types,...dictionary.builtins].map(word=>word.toUpperCase()));
 const builtins=new Set(dictionary.builtins.map(word=>word.toUpperCase()));
 const lines=source.split('\n').map(line=>line.replace(/^\s*[A-Za-z_][A-Za-z_0-9]*\s*=\s*([A-Z][A-Z_0-9]*)\s*(?=\()/,(all,name)=>builtins.has(name)?name:all));
 let text=lines.join('\n');let result='';let i=0;
 while(i<text.length){
  const rest=text.slice(i);const char=text[i];
  if(char==='"'){
   let end=i+1;while(end<text.length){if(text[end]==='"'){if(text[end+1]==='"'){end+=2;continue;}end++;break;}end++;}
   result+=text.slice(i,end);i=end;continue;
  }
  if(char==="'"){const end=text.indexOf('\n',i);if(end<0){result+=rest;break;}result+=text.slice(i,end);i=end;continue;}
  const placeholder=rest.match(/^<[^<>\n]+>/);
  if(placeholder){result+='<'+formalParameterName(placeholder[0].slice(1,-1))+'>';i+=placeholder[0].length;continue;}
  const word=rest.match(/^[A-Za-z_][A-Za-z_0-9]*/);
  if(word){
   const name=word[0];const upper=name.toUpperCase();const call=/^\s*\(/.test(rest.slice(name.length));
   result+=name==='New'||name==='IsValid'?name:known.has(upper)&&(name===upper||call||dictionary.types.includes(name))?upper:'<'+formalParameterName(name)+'>';
   i+=name.length;continue;
  }
  result+=char;i++;
 }
 return result;
}
export function normalizeManualSyntax(html,dictionary){
 const stack=[];
 return html.replace(/<h([234])\b[^>]*>([\s\S]*?)<\/h\1>|<pre><code class="language-axiom">([\s\S]*?)<\/code><\/pre>/g,(match,rank,title,code)=>{
  if(rank){while(stack.length&&stack.at(-1).rank>=Number(rank))stack.pop();stack.push({rank:Number(rank),syntax:/^Синтаксис/.test(title.replace(/<[^>]*>/g,'').trim())});return match;}
  if(!stack.some(heading=>heading.syntax))return match;
  const normalized=normalizeOptionalNotation(formal(decode(code),dictionary));
  const lines=normalized.split('\n').filter(line=>line.trim());
  const separate=lines.length>1&&lines.every(line=>/^[A-Z][A-Z_0-9]*\s*(?:\(|STEP\b|<)/.test(line)&&!/^\s*(?:FOR|IF|WHILE|DO|SUB|FUNCTION|TYPE|SELECT|TRY|ELSE|CASE|END|LOOP|NEXT|RETURN)\b/.test(line));
  return (separate?lines:[normalized]).map(value=>'<pre><code class="language-axiom">'+escape(value)+'</code></pre>').join('\n');
 });
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');const dir=path.join(root,'docs/manual');
 const snapshot=JSON.parse(fs.readFileSync(path.join(dir,'data/language-reference.json'),'utf8'));
 const dictionary={keywords:snapshot.keywords,types:snapshot.types,builtins:snapshot.functions.map(fn=>fn.name)};
 let count=0;
 for(const file of fs.readdirSync(dir).filter(file=>file.endsWith('.html'))){const target=path.join(dir,file);const original=fs.readFileSync(target,'utf8');const normalized=normalizeManualSyntax(original,dictionary);if(normalized!==original){fs.writeFileSync(target,normalized);count++;}}
 console.log('Единое оформление формальных записей: '+count+' страниц.');
}
