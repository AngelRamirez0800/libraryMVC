Coding Standards aplicado al Proyecto

# Practica aplicada
- Se hace uso de `.editorconfig`, es un estándar en la industria, y tiene altas capacidades para sentar reglas el formatting y linting
- Se hace uso del comando `dotnet format` para lanzar el proceso de formatting, tomando de base las reglas declaradas en el archivo `.editorconfig`.
- Se hace uso de `Directory.Build.props` para levantar los Analyzers que provee el SDK de .NET por defecto, en esa ocasión, están tomando de referencias las reglas declaradas en `.editorconfig`
- El formatting y linting es aplicado mediante la ejecución del siguiente comando `dotnet format libraryMVC.csproj` 

# Problemas resueltos

Utilizar dotnet format, tomando de base el archivo .editorconfig permite evitar:
-  Errores de formateo del código
-  Evitar inconsistencias de nomenclatura en variables, métodos, constantes, métodos privados, interfaces, declaraciones
-  Se evita conflictos innecesarios entre desarrollados al declarar tanto variables, métodos, constantes, métodos privados, interfaces.
-  
# Relación con lo discutido en clases
- Este repositorio se esta trabajando en solitario, pero se esta aplicando practicas de branching y commits estrategico para evitar implementaciones gigantescas y capturar de forma eficiente mediante los commits alguna inserción de algun bug en el código
- Se reestructuro el proyecto para aplicar de forma mas estricta principios SOLID, IoC y DI y permitir una implementación mas modular de nuevas funcionalidades y reducir retrabajo 


# Comandos utilizados
 ```bash 
  dotnet format src/libraryMVC/libraryMVC.csproj
  dotnet build src/libraryMVC/libraryMVC.csproj
 ```

## Exportación de reportes

Las listas de libros, autores y categorías permiten descargar el conjunto completo
de registros en PDF o Excel (XLSX), incluidos los inactivos. Los reportes de libros
incluyen autores, categorías, vista previa y cantidad de imágenes; los reportes de
autores y categorías muestran sus libros relacionados. Las asociaciones se
presentan como nombres en una sola fila por entidad.

Las listas se consultan en páginas de 15 elementos y cargan más resultados al
desplazarse, anexándolos a la tabla. Los filtros de lista se aplican antes de
paginación: nombre/nacionalidad para autores, título o ISBN y autor/categoría
para libros, y nombre para categorías. Al solicitar un reporte desde una lista
filtrada, PDF y Excel exportan todos los registros que coinciden con esos filtros.
Los reportes de libros incrustan una miniatura de la primera imagen disponible y
señalan el número de imágenes; el nombre de archivo no se exporta. Si no se
encuentra una imagen disponible, el reporte lo indica.

Los PDF incluyen el nombre del sistema, título, fecha/hora de generación, tabla y
número de página. Los archivos XLSX contienen una hoja por entidad con filtros,
filas superiores congeladas, contenido ajustado en celdas y encabezados
repetidos al imprimir en varias páginas. Todas las descargas de reportes y sus
botones están restringidos a los roles `Admin` y `Librarian`. La generación PDF utiliza QuestPDF con la licencia Community
configurada en el arranque de la aplicación; antes de un despliegue comercial,
confirma que el uso cumple los términos vigentes de QuestPDF.

## Seed de literatura para probar reportes

El catálogo de prueba incluye a Franz Kafka, Arthur Conan Doyle y Fyodor
Dostoevsky, 21 libros (siete por autor), 27 categorías y 15 imágenes de libros.
Se puede ejecutar explícitamente contra la conexión `DefaultConnection`:

```powershell
dotnet run --project src\libraryMVC\libraryMVC.csproj -- --seed-literature
```

El comando aplica primero las migraciones pendientes y luego crea o reutiliza los
autores, libros y categorías; no se ejecuta al iniciar normalmente y rechaza el
entorno `Testing`. La migración agrega unicidad para ISBN e identidad natural de
autor; si detecta duplicados preexistentes, aborta sin eliminarlos ni fusionarlos.
Haz una copia de seguridad antes de ejecutarlo en una base con datos importantes.
Los retratos se guardan como archivos estáticos (el modelo `Author` no tiene una
relación de imagen 1:1); las imágenes históricas de libros se asocian mediante
`BookImage`. No todas las obras tienen imagen. Las fuentes, licencias y obras
representadas se documentan en [literature-seed-provenance.md](licenses/literature-seed-provenance.md).

 # Suite de testing

 La aplicación y sus pruebas están separadas en `src/` y `tests/`. Para compilar y
 ejecutar la suite:

 ```bash
 dotnet build libraryMVC.slnx
 dotnet test tests/libraryMVC.Tests/libraryMVC.Tests.csproj --filter FullyQualifiedName~Unit
 dotnet test tests/libraryMVC.Tests/libraryMVC.Tests.csproj --filter FullyQualifiedName~Integration
 dotnet test tests/libraryMVC.Tests/libraryMVC.Tests.csproj
 ```

 Las pruebas unitarias no requieren servicios externos. Las pruebas de
 integración usan SQL Server mediante Testcontainers y requieren Docker Desktop
 activo; se ejecutan localmente y no se ha configurado CI. El factory de MVC
 sustituye la cadena de conexión, por lo que las pruebas no usan la base local.
 Cada test de integración recibe una base restablecida y el dataset Bogus
 determinista: 2 autores, 10 libros, 5 categorías y una imagen física temporal
 por libro. Las asociaciones Autor-Categoría son indirectas a través de libros.
 Los archivos viven fuera de `wwwroot` y se limpian por test.

 La suite comprueba controllers, repositorios, servicios, modelos y rendering
 MVC con AngleSharp; incluye CRUD HTTP con antiforgery y autorización. Las
 vistas parciales se verifican a través de las páginas que las componen.
 Las pruebas unitarias cubren el servicio genérico, servicios de dominio,
 validación y almacenamiento concurrente de imágenes. `RepositoryIntegrationTests`
 verifica los repositorios concretos, relaciones, restricciones del modelo EF
 Core y la unicidad ante inserciones concurrentes. Los flujos HTTP renderizan las
 vistas accesibles y la prueba de vistas cubre también `Authors/Delete.cshtml`,
 que no tiene una acción/ruta MVC asociada.
