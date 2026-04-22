# Project Context: SA Architect Post-Design Automation Portal

## 1. Project Vision

A SaaS portal designed to automate the tedious "post-design admin" phase for South African architects. Many architects use software like Revit LT, which lacks a 3rd-party API. Therefore, this portal uses exported **IFC (Industry Foundation Classes)** files as the single source of truth. The system extracts 3D geometry and BIM metadata to automatically calculate municipal zoning compliance, SANS 10400-XA energy requirements, and generate schedules.

## 2. Tech Stack

- **Frontend:** Vue 3 (Composition API)
- **3D Web Viewer:** IFC.js / @thatopen/components (for rendering IFC files in the browser)
- **Backend:** C# / .NET Web API
- **IFC Processing Engine:** xBIM Toolkit (`Xbim.Essentials`) in C#
- **Database, Auth, & Storage:** Supabase (PostgreSQL)

## 3. System Architecture & Data Flow

1.  **Client/Setup:** User creates a project in the Vue 3 frontend and inputs manual variables (Erf size, Climatic Zone).
2.  **Upload:** User uploads an `.ifc` file directly to Supabase Storage.
3.  **Visualization:** Frontend renders the uploaded `.ifc` file in the browser using IFC.js.
4.  **Processing Trigger:** Frontend sends a POST request to the C# backend with the Supabase file URL and Project ID.
5.  **Data Extraction:** C# backend downloads the file, uses xBIM to parse the entities (Spaces, Windows, Doors, Elements), calculates areas/counts, and compiles the data.
6.  **Persistence:** C# backend saves the extracted JSON data to the Supabase PostgreSQL database.
7.  **Delivery:** Vue 3 frontend fetches the processed data, displays it on a dashboard, and allows the user to export to PDF or Excel.

## 4. Domain Terminology (South African Context)

To assist with context, the following domain-specific terms are used in this project:

- **IFC:** Industry Foundation Classes. An open, XML/STEP-based file format containing 3D BIM geometry and metadata.
- **Erf:** The South African term for a plot or stand of land.
- **SANS 10400-XA:** South African National Building Regulations regarding energy usage. A key calculation is the Fenestration Ratio (total glass area divided by total net floor area).
- **Coverage:** The footprint area of the building expressed as a percentage of the total Erf size.
- **F.A.R. (Floor Area Ratio):** The total combined floor area of all storeys divided by the Erf size.
- **BOQ (Bill of Quantities):** A list of materials and counts for costing (e.g., doors, windows, electrical fixtures).
- **JBCC / SACAP:** South African architectural and construction contract/regulatory bodies.

## 5. Core MVP Features

1.  **Project Setup:** UI for project creation, input of Erf data, and `.ifc` file upload.
2.  **3D Web Viewer:** In-browser verification of the uploaded 3D model.
3.  **Energy Compliance (SANS 10400-XA):** Automated extraction of `IfcSpace` (rooms) and glass `IfcWindow`/`IfcDoor` entities to calculate the fenestration-to-floor-area percentage.
4.  **Schedules Generator:** Extraction of dimensions, counts, and metadata for doors and windows, formatted into tabular grids.
5.  **Zoning Calculator:** Calculation of Coverage % and F.A.R. based on building footprint vs. manual Erf input.
6.  **Electrical/Fixture Tally:** Automated counting of specific IFC entities (e.g., lights, outlets) to act as a mini-BOQ.
7.  **Auto-Specification Compiler:** Dynamic generation of text-based construction notes based on the specific materials detected in the IFC file.
8.  **Document Export Engine:** Formatting the extracted data into downloadable, council-ready PDFs and QS-ready Excel sheets.

## 6. Architectural Constraints

- **No Direct Plugins:** We cannot write plugins for the user's CAD software (Revit LT constraint). All data _must_ be derived solely from the uploaded `.ifc` file.
- **Heavy Workloads:** IFC parsing is memory-intensive. The C# backend must handle file processing asynchronously to prevent HTTP timeouts.
  p
