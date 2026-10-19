const {test}=require('node:test');const assert=require('node:assert/strict');
const {validateProjectDocuments}=require('../src/main/project-snapshots');
const root='C:\\project\\main.axm';
const document={documentId:'helper',documentRevision:1,sourceFilePath:'C:\\project\\helper.axm',sourceText:'PRINT 2'};
test('Пакет снимка принимает именованную открытую зависимость без проверки диска',()=>{
 assert.equal(validateProjectDocuments(root,[document],'main'),true);
 assert.equal(validateProjectDocuments(root,undefined,'main'),true);
});
test('Пакет снимка отвергает повторы, чужую папку и неверную редакцию',()=>{
 for(const documents of [[document,document],[{...document,documentRevision:-1}],[{...document,sourceFilePath:'C:\\other\\helper.axm'}],
  [{...document,documentId:'main'}],[{...document,sourceFilePath:root}],[{...document,sourceText:null}],{}])
  assert.equal(validateProjectDocuments(root,documents,'main'),false);
});
