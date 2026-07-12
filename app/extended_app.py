from datetime import datetime
from typing import Optional

from fastapi import FastAPI, Form, HTTPException, Request
from fastapi.responses import RedirectResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from sqlalchemy import Boolean, Column, DateTime, Float, ForeignKey, Integer, String, Text, create_engine
from sqlalchemy.orm import declarative_base, relationship, sessionmaker

DATABASE_URL = "sqlite:///./mohandseto.db"
engine = create_engine(DATABASE_URL, connect_args={"check_same_thread": False})
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()


class Company(Base):
    __tablename__ = "companies"
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), nullable=False)
    email = Column(String(255), unique=True, index=True)
    phone = Column(String(50), default="")
    address = Column(Text, default="")
    tax_number = Column(String(100), default="")
    credit_limit = Column(Float, default=0.0)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    branches = relationship("Branch", back_populates="company", cascade="all, delete-orphan")
    rfqs = relationship("RFQ", back_populates="company", cascade="all, delete-orphan")
    orders = relationship("Order", back_populates="company", cascade="all, delete-orphan")
    invoices = relationship("Invoice", back_populates="company", cascade="all, delete-orphan")
    tickets = relationship("Ticket", back_populates="company", cascade="all, delete-orphan")
    users = relationship("User", back_populates="company", cascade="all, delete-orphan")


class Branch(Base):
    __tablename__ = "branches"
    id = Column(Integer, primary_key=True, index=True)
    company_id = Column(Integer, ForeignKey("companies.id"), nullable=False)
    name = Column(String(255), nullable=False)
    city = Column(String(100), default="")
    address = Column(Text, default="")
    company = relationship("Company", back_populates="branches")


class Product(Base):
    __tablename__ = "products"
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), nullable=False)
    code = Column(String(100), unique=True, index=True)
    price = Column(Float, default=0.0)
    category = Column(String(100), default="General")
    description = Column(Text, default="")
    stock_quantity = Column(Integer, default=0)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)


class RFQ(Base):
    __tablename__ = "rfqs"
    id = Column(Integer, primary_key=True, index=True)
    company_id = Column(Integer, ForeignKey("companies.id"), nullable=False)
    title = Column(String(255), nullable=False)
    request_date = Column(DateTime, default=datetime.utcnow)
    status = Column(String(50), default="Pending")
    notes = Column(Text, default="")
    company = relationship("Company", back_populates="rfqs")


class Order(Base):
    __tablename__ = "orders"
    id = Column(Integer, primary_key=True, index=True)
    company_id = Column(Integer, ForeignKey("companies.id"), nullable=False)
    order_number = Column(String(100), unique=True, index=True)
    order_date = Column(DateTime, default=datetime.utcnow)
    status = Column(String(50), default="Pending")
    total_amount = Column(Float, default=0.0)
    notes = Column(Text, default="")
    company = relationship("Company", back_populates="orders")


class Invoice(Base):
    __tablename__ = "invoices"
    id = Column(Integer, primary_key=True, index=True)
    company_id = Column(Integer, ForeignKey("companies.id"), nullable=False)
    invoice_number = Column(String(100), unique=True, index=True)
    issue_date = Column(DateTime, default=datetime.utcnow)
    due_date = Column(DateTime, default=datetime.utcnow)
    amount = Column(Float, default=0.0)
    status = Column(String(50), default="Issued")
    notes = Column(Text, default="")
    company = relationship("Company", back_populates="invoices")


class Ticket(Base):
    __tablename__ = "tickets"
    id = Column(Integer, primary_key=True, index=True)
    company_id = Column(Integer, ForeignKey("companies.id"), nullable=False)
    subject = Column(String(255), nullable=False)
    priority = Column(String(50), default="Medium")
    status = Column(String(50), default="Open")
    message = Column(Text, default="")
    created_at = Column(DateTime, default=datetime.utcnow)
    company = relationship("Company", back_populates="tickets")


class User(Base):
    __tablename__ = "users"
    id = Column(Integer, primary_key=True, index=True)
    company_id = Column(Integer, ForeignKey("companies.id"), nullable=True)
    name = Column(String(255), nullable=False)
    email = Column(String(255), unique=True, index=True)
    role = Column(String(50), default="Manager")
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    company = relationship("Company", back_populates="users")


def ensure_schema():
    Base.metadata.create_all(bind=engine)
    with engine.begin() as conn:
        def table_exists(table_name: str) -> bool:
            rows = conn.exec_driver_sql(
                f"SELECT name FROM sqlite_master WHERE type='table' AND name='{table_name}'"
            ).fetchall()
            return bool(rows)

        def column_exists(table_name: str, column_name: str) -> bool:
            if not table_exists(table_name):
                return False
            rows = conn.exec_driver_sql(f"PRAGMA table_info({table_name})").fetchall()
            return any(row[1] == column_name for row in rows)

        for table_name, columns in {
            "products": [("stock_quantity", "stock_quantity INTEGER DEFAULT 0"), ("is_active", "is_active INTEGER DEFAULT 1"), ("created_at", "created_at DATETIME DEFAULT CURRENT_TIMESTAMP")],
            "companies": [("tax_number", "tax_number VARCHAR(100) DEFAULT ''"), ("credit_limit", "credit_limit FLOAT DEFAULT 0.0"), ("is_active", "is_active INTEGER DEFAULT 1"), ("created_at", "created_at DATETIME DEFAULT CURRENT_TIMESTAMP")],
            "rfqs": [("request_date", "request_date DATETIME DEFAULT CURRENT_TIMESTAMP"), ("status", "status VARCHAR(50) DEFAULT 'Pending'"), ("notes", "notes TEXT DEFAULT ''")],
            "orders": [("order_date", "order_date DATETIME DEFAULT CURRENT_TIMESTAMP"), ("status", "status VARCHAR(50) DEFAULT 'Pending'"), ("total_amount", "total_amount FLOAT DEFAULT 0.0"), ("notes", "notes TEXT DEFAULT ''")],
            "invoices": [("issue_date", "issue_date DATETIME DEFAULT CURRENT_TIMESTAMP"), ("due_date", "due_date DATETIME DEFAULT CURRENT_TIMESTAMP"), ("amount", "amount FLOAT DEFAULT 0.0"), ("status", "status VARCHAR(50) DEFAULT 'Issued'"), ("notes", "notes TEXT DEFAULT ''")],
            "tickets": [("priority", "priority VARCHAR(50) DEFAULT 'Medium'"), ("status", "status VARCHAR(50) DEFAULT 'Open'"), ("message", "message TEXT DEFAULT ''"), ("created_at", "created_at DATETIME DEFAULT CURRENT_TIMESTAMP")],
            "users": [("company_id", "company_id INTEGER"), ("role", "role VARCHAR(50) DEFAULT 'Manager'"), ("is_active", "is_active INTEGER DEFAULT 1"), ("created_at", "created_at DATETIME DEFAULT CURRENT_TIMESTAMP")],
        }.items():
            for column_name, definition in columns:
                if not column_exists(table_name, column_name):
                    conn.exec_driver_sql(f"ALTER TABLE {table_name} ADD COLUMN {definition}")


ensure_schema()

app = FastAPI(title="Mohandseto Tawredat", version="0.3.0")
app.mount("/static", StaticFiles(directory="app/static"), name="static")
templates = Jinja2Templates(directory="app/templates")


def seed_data():
    db = SessionLocal()
    try:
        if db.query(Product).count() == 0:
            db.add_all([
                Product(name="ورقة A4", code="P-1001", price=12.5, category="مكتبية", description="ورقة طباعة", stock_quantity=120),
                Product(name="حبر طباعة", code="P-1002", price=85.0, category="إلكترونيات", description="حبر أسود", stock_quantity=50),
                Product(name="أقلام حبر", code="P-1003", price=8.0, category="مكتبية", description="أقلام كتابة", stock_quantity=300),
            ])
        if db.query(Company).count() == 0:
            db.add_all([
                Company(name="شركة النور", email="info@alnoor.com", phone="01000000000", address="القاهرة", tax_number="123456789", credit_limit=50000),
                Company(name="شركة الراية", email="sales@alrayah.com", phone="01111111111", address="الجيزة", tax_number="987654321", credit_limit=75000),
            ])
        if db.query(RFQ).count() == 0:
            db.add_all([
                RFQ(company_id=1, title="طلب طباعة جديد", status="Pending", notes="مطلوب توريد خلال 5 أيام"),
                RFQ(company_id=2, title="احتياجات مكتبية", status="Approved", notes="سيتم التنفيذ خلال الأسبوع"),
            ])
        if db.query(Order).count() == 0:
            db.add_all([
                Order(company_id=1, order_number="ORD-1001", status="Pending", total_amount=1250.0, notes="طلب أولي"),
                Order(company_id=2, order_number="ORD-1002", status="Completed", total_amount=4100.0, notes="تم التوريد"),
            ])
        if db.query(Invoice).count() == 0:
            db.add_all([
                Invoice(company_id=1, invoice_number="INV-1001", amount=1250.0, status="Issued", notes="فاتورة أولية"),
                Invoice(company_id=2, invoice_number="INV-1002", amount=4100.0, status="Paid", notes="تم الدفع"),
            ])
        if db.query(Ticket).count() == 0:
            db.add_all([
                Ticket(company_id=1, subject="طلب دعم فني", priority="High", status="Open", message="يوجد مشكلة في التسليم"),
                Ticket(company_id=2, subject="استفسار عن فاتورة", priority="Medium", status="Closed", message="تم الرد"),
            ])
        if db.query(User).count() == 0:
            db.add_all([
                User(company_id=1, name="أحمد", email="ahmed@company.com", role="Admin", is_active=True),
                User(company_id=2, name="سارة", email="sara@company.com", role="Sales", is_active=True),
            ])
        db.commit()
    finally:
        db.close()


seed_data()


@app.get("/")
def dashboard(request: Request):
    db = SessionLocal()
    try:
        stats = {
            "companies": db.query(Company).count(),
            "products": db.query(Product).count(),
            "rfqs": db.query(RFQ).count(),
            "orders": db.query(Order).count(),
            "invoices": db.query(Invoice).count(),
            "tickets": db.query(Ticket).count(),
            "users": db.query(User).count(),
        }
        recent_orders = db.query(Order).order_by(Order.id.desc()).limit(5).all()
        recent_rfqs = db.query(RFQ).order_by(RFQ.id.desc()).limit(5).all()
        recent_tickets = db.query(Ticket).order_by(Ticket.id.desc()).limit(5).all()
        return templates.TemplateResponse("dashboard.html", {"request": request, "stats": stats, "recent_orders": recent_orders, "recent_rfqs": recent_rfqs, "recent_tickets": recent_tickets})
    finally:
        db.close()


@app.get("/crm")
def crm_page(request: Request):
    db = SessionLocal()
    try:
        companies = db.query(Company).order_by(Company.id.desc()).all()
        tickets = db.query(Ticket).order_by(Ticket.id.desc()).all()
        rfqs = db.query(RFQ).order_by(RFQ.id.desc()).all()
        return templates.TemplateResponse("crm.html", {"request": request, "companies": companies, "tickets": tickets, "rfqs": rfqs})
    finally:
        db.close()


@app.get("/products")
def products_page(request: Request):
    db = SessionLocal()
    try:
        products = db.query(Product).order_by(Product.id.desc()).all()
        return templates.TemplateResponse("products.html", {"request": request, "products": products})
    finally:
        db.close()


@app.post("/products")
def create_product(name: str = Form(...), code: str = Form(...), price: float = Form(...), category: str = Form(...), description: str = Form(default=""), stock_quantity: int = Form(default=0)):
    db = SessionLocal()
    try:
        product = Product(name=name, code=code, price=price, category=category, description=description, stock_quantity=stock_quantity, is_active=True)
        db.add(product)
        db.commit()
        db.refresh(product)
    finally:
        db.close()
    return RedirectResponse(url="/products", status_code=303)


@app.get("/companies")
def companies_page(request: Request):
    db = SessionLocal()
    try:
        companies = db.query(Company).order_by(Company.id.desc()).all()
        return templates.TemplateResponse("companies.html", {"request": request, "companies": companies})
    finally:
        db.close()


@app.post("/companies")
def create_company(name: str = Form(...), email: str = Form(...), phone: str = Form(default=""), address: str = Form(default=""), tax_number: str = Form(default=""), credit_limit: float = Form(default=0.0)):
    db = SessionLocal()
    try:
        company = Company(name=name, email=email, phone=phone, address=address, tax_number=tax_number, credit_limit=credit_limit, is_active=True)
        db.add(company)
        db.commit()
        db.refresh(company)
    finally:
        db.close()
    return RedirectResponse(url="/companies", status_code=303)


@app.get("/rfqs")
def rfqs_page(request: Request):
    db = SessionLocal()
    try:
        rfqs = db.query(RFQ).order_by(RFQ.id.desc()).all()
        companies = db.query(Company).all()
        return templates.TemplateResponse("rfqs.html", {"request": request, "rfqs": rfqs, "companies": companies})
    finally:
        db.close()


@app.post("/rfqs")
def create_rfq(title: str = Form(...), company_id: int = Form(...), notes: str = Form(default="")):
    db = SessionLocal()
    try:
        rfq = RFQ(title=title, company_id=company_id, notes=notes, status="Pending")
        db.add(rfq)
        db.commit()
        db.refresh(rfq)
    finally:
        db.close()
    return RedirectResponse(url="/rfqs", status_code=303)


@app.get("/orders")
def orders_page(request: Request):
    db = SessionLocal()
    try:
        orders = db.query(Order).order_by(Order.id.desc()).all()
        companies = db.query(Company).all()
        return templates.TemplateResponse("orders.html", {"request": request, "orders": orders, "companies": companies})
    finally:
        db.close()


@app.post("/orders")
def create_order(order_number: str = Form(...), company_id: int = Form(...), total_amount: float = Form(default=0.0), notes: str = Form(default="")):
    db = SessionLocal()
    try:
        order = Order(order_number=order_number, company_id=company_id, total_amount=total_amount, notes=notes, status="Pending")
        db.add(order)
        db.commit()
        db.refresh(order)
    finally:
        db.close()
    return RedirectResponse(url="/orders", status_code=303)


@app.get("/invoices")
def invoices_page(request: Request):
    db = SessionLocal()
    try:
        invoices = db.query(Invoice).order_by(Invoice.id.desc()).all()
        companies = db.query(Company).all()
        return templates.TemplateResponse("invoices.html", {"request": request, "invoices": invoices, "companies": companies})
    finally:
        db.close()


@app.post("/invoices")
def create_invoice(invoice_number: str = Form(...), company_id: int = Form(...), amount: float = Form(default=0.0), status: str = Form(default="Issued"), notes: str = Form(default="")):
    db = SessionLocal()
    try:
        invoice = Invoice(invoice_number=invoice_number, company_id=company_id, amount=amount, status=status, notes=notes)
        db.add(invoice)
        db.commit()
        db.refresh(invoice)
    finally:
        db.close()
    return RedirectResponse(url="/invoices", status_code=303)


@app.get("/tickets")
def tickets_page(request: Request):
    db = SessionLocal()
    try:
        tickets = db.query(Ticket).order_by(Ticket.id.desc()).all()
        companies = db.query(Company).all()
        return templates.TemplateResponse("tickets.html", {"request": request, "tickets": tickets, "companies": companies})
    finally:
        db.close()


@app.post("/tickets")
def create_ticket(subject: str = Form(...), company_id: int = Form(...), priority: str = Form(default="Medium"), status: str = Form(default="Open"), message: str = Form(default="")):
    db = SessionLocal()
    try:
        ticket = Ticket(subject=subject, company_id=company_id, priority=priority, status=status, message=message)
        db.add(ticket)
        db.commit()
        db.refresh(ticket)
    finally:
        db.close()
    return RedirectResponse(url="/tickets", status_code=303)


@app.get("/users")
def users_page(request: Request):
    db = SessionLocal()
    try:
        users = db.query(User).order_by(User.id.desc()).all()
        companies = db.query(Company).all()
        return templates.TemplateResponse("users.html", {"request": request, "users": users, "companies": companies})
    finally:
        db.close()


@app.post("/users")
def create_user(name: str = Form(...), email: str = Form(...), company_id: int = Form(default=0), role: str = Form(default="Manager")):
    db = SessionLocal()
    try:
        user = User(name=name, email=email, company_id=company_id or None, role=role, is_active=True)
        db.add(user)
        db.commit()
        db.refresh(user)
    finally:
        db.close()
    return RedirectResponse(url="/users", status_code=303)


@app.get("/reports")
def reports_page(request: Request):
    db = SessionLocal()
    try:
        stats = {
            "companies": db.query(Company).count(),
            "products": db.query(Product).count(),
            "orders": db.query(Order).count(),
            "invoices": db.query(Invoice).count(),
            "tickets": db.query(Ticket).count(),
        }
        return templates.TemplateResponse("reports.html", {"request": request, "stats": stats})
    finally:
        db.close()


@app.get("/api/dashboard")
def dashboard_api():
    db = SessionLocal()
    try:
        return {
            "companies": db.query(Company).count(),
            "products": db.query(Product).count(),
            "rfqs": db.query(RFQ).count(),
            "orders": db.query(Order).count(),
            "invoices": db.query(Invoice).count(),
            "tickets": db.query(Ticket).count(),
            "users": db.query(User).count(),
        }
    finally:
        db.close()


@app.get("/api/products")
def list_products():
    db = SessionLocal()
    try:
        return [{"id": p.id, "name": p.name, "code": p.code, "price": p.price, "category": p.category, "description": p.description, "stock_quantity": p.stock_quantity, "is_active": p.is_active} for p in db.query(Product).all()]
    finally:
        db.close()


@app.get("/api/companies")
def list_companies():
    db = SessionLocal()
    try:
        return [{"id": c.id, "name": c.name, "email": c.email, "phone": c.phone, "address": c.address, "credit_limit": c.credit_limit, "is_active": c.is_active} for c in db.query(Company).all()]
    finally:
        db.close()


@app.get("/api/orders")
def list_orders():
    db = SessionLocal()
    try:
        return [{"id": o.id, "order_number": o.order_number, "status": o.status, "total_amount": o.total_amount, "company_id": o.company_id} for o in db.query(Order).all()]
    finally:
        db.close()


@app.get("/api/invoices")
def list_invoices():
    db = SessionLocal()
    try:
        return [{"id": i.id, "invoice_number": i.invoice_number, "amount": i.amount, "status": i.status, "company_id": i.company_id} for i in db.query(Invoice).all()]
    finally:
        db.close()


@app.get("/api/tickets")
def list_tickets():
    db = SessionLocal()
    try:
        return [{"id": t.id, "subject": t.subject, "priority": t.priority, "status": t.status, "company_id": t.company_id} for t in db.query(Ticket).all()]
    finally:
        db.close()


@app.get("/api/users")
def list_users():
    db = SessionLocal()
    try:
        return [{"id": u.id, "name": u.name, "email": u.email, "role": u.role, "company_id": u.company_id, "is_active": u.is_active} for u in db.query(User).all()]
    finally:
        db.close()
