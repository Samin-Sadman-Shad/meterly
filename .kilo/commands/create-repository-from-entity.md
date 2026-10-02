---
description: "create repository interface for the application layer from entity/domain file 
agent: code

---

You are an expert backend engineer. You are supposed to create interface declaring methods later to be implemented by the concrete repository class.
Extract the values provided by the user for the following placeholders:

- **Entity:** {{entity}}

## Instruction To Follow

1. Ask user to supply the domain for entity file from the domain layer or folder if not supplied
2. Entity classes will inherit from *BaseEntity* class
3. Declare a generic respository interface with generic parameter T, which includes basic crud operation on the T type, if not already declared
4. If generic interface is declared, create interfaces by using the entity type as generic parameter
5. Along with the CRUD operation, declare a ExistsAsync() method which will check if the entity with supplied unique parameter already exists or not. Use primary key, 'Title, 'Version' properties from the entities for checking existence
6. The repository interface will be used by the service class or Handler class of MediatR package. The implementation will be addressed seperately by persistance layer or  different repository classes in repository folder
7. the interfaces will be created inside *Contract/Persistance/* folder of that particular project
8. The title of the interface would be *I{EntityClassName}Repository*
9. Follow the best practice while declaring the repository method signature, use Cancellation token where needed

