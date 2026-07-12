from datetime import datetime
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


def ensure_schema():
    Base.metadata.create_all(bind=engine)
    with engine.begin() as conn:
        def table_exists(table_name: str) -> bool:
            rows = conn.exec_driver_sql(f"SELECT name FROM sqlite_master WHERE type='table' AND name='{table_name}'").fetchall()
            return bool(rows)

        def column_exists(table_name: str, column_name: str) -> bool:
            if not table_exists(table_name):
                return False
            rows = conn.exec_driver_sql(f"PRAGMA table_info({table_name})").fetchall()
            return any(row[1] == column_name for row in rows)

        if table_exists("products"):
            for column_name, definition in [
                ("stock_quantity", "stock_quantity INTEGER DEFAULT 0"),
                ("is_active", "is_active INTEGER DEFAULT 1"),
                ("created_at", "created_at DATETIME DEFAULT CURRENT_TIMESTAMP"),
            ]:
                if not column_exists("products", column_name):
                    conn.exec_driver_sql(f"ALTER TABLE products ADD COLUMN {definition}")

        if table_exists("companies"):
            for column_name, definition in [
                ("tax_number", "tax_number VARCHAR(100) DEFAULT ''"),
                ("credit_limit", "credit_limit FLOAT DEFAULT 0.0"),
                ("is_active", "is_active INTEGER DEFAULT 1"),
                ("created_at", "created_at DATETIME DEFAULT CURRENT_TIMESTAMP"),
            ]:
                if not column_exists("companies", column_name):
                    conn.exec_driver_sql(f"ALTER TABLE companies ADD COLUMN {definition}")

        if table_exists("rfqs"):
            for column_name, definition in [
                ("request_date", "request_date DATETIME DEFAULT CURRENT_TIMESTAMP"),
                ("status", "status VARCHAR(50) DEFAULT 'Pending'"),
                ("notes", "notes TEXT DEFAULT ''"),
            ]:
                if not column_exists("rfqs", column_name):
                    conn.exec_driver_sql(f"ALTER TABLE rfqs ADD COLUMN {definition}")

        if table_exists("orders"):
            for column_name, definition in [
                ("order_date", "order_date DATETIME DEFAULT CURRENT_TIMESTAMP"),
                ("status", "status VARCHAR(50) DEFAULT 'Pending'"),
                ("total_amount", "total_amount FLOAT DEFAULT 0.0"),
                ("notes", "notes TEXT DEFAULT ''"),
            ]:
                if not column_exists("orders", column_name):
                    conn.exec_driver_sql(f"ALTER TABLE orders ADD COLUMN {definition}")


ensure_schema()

app = FastAPI(title="Mohandseto Tawredat", version="0.2.0")
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
        }
        recent_orders = db.query(Order).order_by(Order.id.desc()).limit(5).all()
        recent_rfqs = db.query(RFQ).order_by(RFQ.id.desc()).limit(5).all()
        return templates.TemplateResponse("dashboard.html", {"request": request, "stats": stats, "recent_orders": recent_orders, "recent_rfqs": recent_rfqs})
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
def create_product(
    name: str = Form(...),
    code: str = Form(...),
    price: float = Form(...),
    category: str = Form(...),
    description: str = Form(default=""),
    stock_quantity: int = Form(default=0),
):
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
def create_company(
    name: str = Form(...),
    email: str = Form(...),
    phone: str = Form(default=""),
    address: str = Form(default=""),
    tax_number: str = Form(default=""),
    credit_limit: float = Form(default=0.0),
):
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


@app.get("/api/dashboard")
def dashboard_api():
    db = SessionLocal()
    try:
        return {
            "companies": db.query(Company).count(),
            "products": db.query(Product).count(),
            "rfqs": db.query(RFQ).count(),
            "orders": db.query(Order).count(),
        }
    finally:
        db.close()


@app.get("/api/products")
def list_products():
    db = SessionLocal()
    try:
        products = db.query(Product).all()
        return [{
            "id": p.id,
            "name": p.name,
            "code": p.code,
            "price": p.price,
            "category": p.category,
            "description": p.description,
            "stock_quantity": p.stock_quantity,
            "is_active": p.is_active,
        } for p in products]
    finally:
        db.close()


@app.post("/api/products", status_code=201)
def create_product_api(payload: dict):
    db = SessionLocal()
    try:
        product = Product(**payload)
        db.add(product)
        db.commit()
        db.refresh(product)
        return {"id": product.id, "message": "created"}
    except Exception as e:
        db.rollback()
        raise HTTPException(status_code=400, detail=str(e))
    finally:
        db.close()


@app.put("/api/products/{product_id}")
def update_product(product_id: int, payload: dict):
    db = SessionLocal()
    try:
        product = db.query(Product).filter(Product.id == product_id).first()
        if not product:
            raise HTTPException(status_code=404, detail="product not found")
        for key, value in payload.items():
            setattr(product, key, value)
        db.commit()
        db.refresh(product)
        return {"id": product.id, "message": "updated"}
    except Exception as e:
        db.rollback()
        raise HTTPException(status_code=400, detail=str(e))
    finally:
        db.close()


@app.delete("/api/products/{product_id}")
def delete_product(product_id: int):
    db = SessionLocal()
    try:
        product = db.query(Product).filter(Product.id == product_id).first()
        if not product:
            raise HTTPException(status_code=404, detail="product not found")
        db.delete(product)
        db.commit()
        return {"message": "deleted"}
    finally:
        db.close()


@app.get("/api/companies")
def list_companies():
    db = SessionLocal()
    try:
        companies = db.query(Company).all()
        return [{
            "id": c.id,
            "name": c.name,
            "email": c.email,
            "phone": c.phone,
            "address": c.address,
            "tax_number": c.tax_number,
            "credit_limit": c.credit_limit,
            "is_active": c.is_active,
        } for c in companies]
    finally:
        db.close()


@app.post("/api/companies", status_code=201)
def create_company_api(payload: dict):
    db = SessionLocal()
    try:
        company = Company(**payload)
        db.add(company)
        db.commit()
        db.refresh(company)
        return {"id": company.id, "message": "created"}
    except Exception as e:
        db.rollback()
        raise HTTPException(status_code=400, detail=str(e))
    finally:
        db.close()


@app.put("/api/companies/{company_id}")
def update_company(company_id: int, payload: dict):
    db = SessionLocal()
    try:
        company = db.query(Company).filter(Company.id == company_id).first()
        if not company:
            raise HTTPException(status_code=404, detail="company not found")
        for key, value in payload.items():
            setattr(company, key, value)
        db.commit()
        db.refresh(company)
        return {"id": company.id, "message": "updated"}
    except Exception as e:
        db.rollback()
        raise HTTPException(status_code=400, detail=str(e))
    finally:
        db.close()


@app.delete("/api/companies/{company_id}")
def delete_company(company_id: int):
    db = SessionLocal()
    try:
        company = db.query(Company).filter(Company.id == company_id).first()
        if not company:
            raise HTTPException(status_code=404, detail="company not found")
        db.delete(company)
        db.commit()
        return {"message": "deleted"}
    finally:
        db.close()
