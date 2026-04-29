Smart View Filter
Version: 2.0.0
Target: Autodesk Revit 2022, 2023, and 2026

Description
Smart View Filter is a Revit add-in that helps users read a source selection, build category/family/type scope trees, create parameter-based filter rules, and then apply, select, or temporarily isolate matching elements.

Main Features
- Read selected Revit elements into a structured source tree.
- Filter by category, family, and type.
- Create multiple parameter rules.
- Combine rules with AND / OR logic.
- Search parameter names while creating rules.
- Filter only the selected source elements or matching project elements.
- Limit results to the active view.
- Live preview match counts while rules and options change.
- Apply rules to see match count.
- Select matching elements in Revit.
- Temporarily isolate matching elements in the active view.
- Save and load filter configurations as local JSON.

Basic Usage
1. Open Revit and open a model.
2. Select one or more source elements in the model.
3. Go to the Smart Revit tab.
4. Click Smart View Filter.
5. Click Read Selection.
6. Choose the source scope from the tree.
7. Choose Selected source only or Project elements.
8. Add one or more filter rules. Use the parameter search box to find parameters quickly.
9. Click Apply, Select, or Isolate.

Saved Configurations
Saved configurations are stored locally at:
%APPDATA%\SmartViewFilter\saved-configurations.json

Notes
- Numeric comparisons use the raw numeric values returned by Revit parameters.
- Text comparisons check instance parameters first and then type parameters when available.
- Isolate uses Revit temporary isolate in the active view.
- No user data is transmitted outside the local computer.
- The app icon contains only graphics, with no text or numbers.

Support
Update this section with your support email before publishing.

Privacy Policy
See PRIVACY_POLICY.txt in this package, or use:
https://github.com/ashishpundir924/Smart-View-Filter/blob/main/PRIVACY_POLICY.md

