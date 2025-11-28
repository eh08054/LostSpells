

# class ChooseAction:
#     def FindActionByNLU(clean_text):
#     skill_evidence = "No Skill"

#     result = {
#         "action": "none",
#         "skill": False,
#         "skill_name": "none"
#     }

#     if("공격" in clean_text):
#         result["action"] = "attack"
#     if("이동" in clean_text):
#         result["action"] = "move"
#     if hasattr(app.state, "skillSet"):
#         for i in app.state.skillSetKorean:
#             print(clean_text, i)
#             if i in clean_text:
#                 print("EXIST SKILL:", i)
#                 skill_evidence = i
#                 result["action"] = "attack"
#                 result["skill"] = True
#                 result["skill_name"] = skill_evidence
#                 break

#     return result
        
