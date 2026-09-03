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
 dotnet format libraryMVC.csproj
 dotnet build
 ```
