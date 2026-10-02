---
description: "create validation file for dtos inside a folder
agent: code

---

You are an expert backend engineer. You are supposed to create Validator classes which will contain validation logic and later used for validation of dto classes. 
Extract the values provided by the user for the following placeholders:
- **Dto:** {{[dto1, dto2, ....]}}

# Instruction To Follow

1. Use FluentValidation package for writing validation script.
2. If FluentValidation is not installed for a project, ask for persmission to install the latest package from the Nuget
3. Write seperate validation class for each dto class found inside a folder passed in the prompt.
4. Every dto class name will be end with 'Dto'. Dto class will be supplied by the user with the prompt
5. The validation class will inherit from AbstractValidator<TDto> with the dto type bing passed as the generic type.
6. Write validation logic for each of the properties in the dto class and use *RuleFor(x => {})* method from the AbstractValidator<TDto> class for implementing the validation logic.
7. If the create Dto class contains properties name with 'Title', 'Id', 'Version', check if the particular entity corresponding to that property already exists in the database via using Exists() or DoesExist() or equivalent in the corresponding repository interface. If Repository interface not found, ask to write down the repository interface name in the prompt. If already exist, do not create a new version of the dto. Make sure any create dto maintaines idempotency. Create dto types implements *ICreateDto* interface.
8. Inject Repository interfaces if the dto class has dependency on them, specially for create or update dto. Use Async methods such as MustAsync(x => ...) for such purpose.
9. Validation classes will be created inside DTOs/<Particular folder for each dto>/Validators/
10. Serach for Dto files inside DTOs folder
11. Apply WithMessage() method to every validation logic
12. Create or use already created static file where the custom message for each validation logic fail will be stored as const string variable. Use these string inside WithMeassge()
