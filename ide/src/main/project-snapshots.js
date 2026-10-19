const path=require('node:path');
function validateProjectDocuments(entryPath,documents,entryDocumentId) {
  if(documents==null) return true;
  if(!Array.isArray(documents)) return false;
  if(!documents.length) return true;
  if(typeof entryPath!=='string'||!path.isAbsolute(entryPath)) return false;
  const entry=path.resolve(entryPath).toLowerCase(),folder=path.dirname(entry);
  const paths=new Set([entry]),identities=new Set(entryDocumentId ? [entryDocumentId] : []);
  for(const document of documents) {
    if(!document||typeof document.documentId!=='string'||!document.documentId.trim()||document.documentId.length>128||
      !Number.isInteger(document.documentRevision)||document.documentRevision<0||document.documentRevision>2147483647||
      typeof document.sourceText!=='string'||typeof document.sourceFilePath!=='string'||!path.isAbsolute(document.sourceFilePath)) return false;
    const file=path.resolve(document.sourceFilePath).toLowerCase();
    if(path.dirname(file)!==folder||path.extname(file)!=='.axm'||paths.has(file)||identities.has(document.documentId)) return false;
    paths.add(file);identities.add(document.documentId);
  }
  return true;
}
module.exports={validateProjectDocuments};
