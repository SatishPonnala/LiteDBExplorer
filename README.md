# LiteDB Explorer - Professional NoSQL Database Viewer

A modern, professional NoSQL database viewer and management tool for LiteDB databases, inspired by Studio 3T. Built with Windows App SDK and featuring a sophisticated dark theme with comprehensive database management capabilities.

![LiteDB Explorer](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue)
![LiteDB Explorer](https://img.shields.io/badge/Framework-Windows%20App%20SDK-green)
![LiteDB Explorer](https://img.shields.io/badge/Theme-Dark%20Mode-orange)

## ✨ Features

### 🎨 Professional UI/UX
- **Modern Dark Theme**: Professional Studio 3T-inspired dark interface
- **Responsive Layout**: Resizable panels and intuitive navigation
- **Breadcrumb Navigation**: Clear database and collection hierarchy
- **Professional Toolbars**: Context-aware action buttons and shortcuts

### 📊 Database Management
- **Database Connection**: Open and manage LiteDB database files
- **Collection Explorer**: Browse and manage database collections
- **Document Viewer**: Advanced JSON document viewing with tree and raw modes
- **Real-time Statistics**: Database size, collection counts, and performance metrics

### 🔍 Advanced Querying
- **Query Editor**: Professional query interface with syntax highlighting
- **Multiple Query Types**: Support for LINQ, JSON, and aggregation queries
- **Query Templates**: Pre-built query templates for common operations
- **Results Export**: Export query results to JSON format

### 📝 Document Management
- **Professional Editor**: Advanced JSON document editor with validation
- **Syntax Highlighting**: Real-time JSON syntax validation and formatting
- **Document Templates**: Quick insertion of common document structures
- **Bulk Operations**: Add, edit, delete, and copy documents

### 🛠️ Developer Features
- **JSON Validation**: Real-time JSON syntax checking
- **Character Counting**: Document size and character statistics
- **Cursor Position**: Line and column position tracking
- **Keyboard Shortcuts**: Professional keyboard navigation

## 🚀 Getting Started

### Prerequisites
- Windows 10/11
- .NET 6.0 or later
- Windows App SDK

### Installation
1. Clone the repository
2. Open the solution in Visual Studio 2022
3. Build and run the application

### Usage
1. **Open Database**: Click "Open Database" to select a LiteDB file
2. **Browse Collections**: Navigate through collections in the sidebar
3. **View Documents**: Select documents to view in the detail panel
4. **Edit Documents**: Use the professional editor for document modifications
5. **Execute Queries**: Use the query editor for advanced database operations

## 🎯 Key Features

### Professional Interface
- **Studio 3T-inspired Design**: Professional dark theme with modern UI elements
- **Resizable Panels**: Adjustable sidebar and detail views
- **Status Bar**: Real-time status information and database statistics
- **Breadcrumb Navigation**: Clear navigation hierarchy

### Advanced Document Management
- **Tree View**: Hierarchical JSON document viewing
- **Raw JSON**: Direct JSON text editing
- **Document Actions**: Quick access to edit, delete, and copy operations
- **Context Menus**: Right-click context menus for document operations

### Query Capabilities
- **Query Editor**: Professional query interface with syntax support
- **Multiple Formats**: Support for LINQ, JSON, and aggregation queries
- **Results Display**: Formatted query results with export capabilities
- **Query History**: Track and manage query execution

### Database Statistics
- **Overview Dashboard**: Database size, collection counts, and document totals
- **Performance Metrics**: Query performance and storage usage
- **Collection Details**: Individual collection statistics and metadata
- **System Information**: Database path, version information, and app details

## 🎨 Theme System

The application features a sophisticated dark theme system inspired by professional database management tools:

- **Dark Background**: Professional dark color scheme
- **Accent Colors**: Blue accent colors for highlights and actions
- **Card-based Layout**: Modern card-based interface design
- **Professional Typography**: Clear, readable fonts and spacing

## ⌨️ Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Open Database | `Ctrl + O` |
| New Document | `Ctrl + N` |
| Delete Document | `Delete` |
| Refresh | `F5` |
| Query Editor | `Ctrl + Q` |
| Export | `Ctrl + E` |

## 🛠️ Development

### Project Structure
```
LiteDBExplorer/
├── Views/                 # UI Views and Dialogs
├── ViewModels/           # MVVM ViewModels
├── Models/               # Data Models
├── Services/             # Business Logic Services
├── Controls/             # Custom Controls
├── Helpers/              # Utility Classes
└── Themes/               # Theme Resources
```

### Key Components
- **MainWindow**: Primary application window with professional layout
- **QueryEditorView**: Advanced query interface
- **DocumentEditorDialog**: Professional JSON document editor
- **DatabaseStatsView**: Comprehensive database statistics
- **JsonViewerControl**: Custom JSON tree viewer

## 📈 Performance Features

- **Real-time Validation**: Instant JSON syntax checking
- **Efficient Rendering**: Optimized document display
- **Memory Management**: Efficient handling of large datasets
- **Background Processing**: Non-blocking database operations

## 🔧 Configuration

The application supports various configuration options:

- **Theme Toggle**: Switch between light and dark themes
- **Panel Sizing**: Adjustable sidebar and detail panel widths
- **View Modes**: Toggle between tree and raw JSON views
- **Display Options**: Customize document display preferences

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Inspired by Studio 3T's professional database management interface
- Built with Windows App SDK and modern .NET technologies
- Uses LiteDB for NoSQL database operations

---

**LiteDB Explorer** - Professional NoSQL Database Viewer for Windows