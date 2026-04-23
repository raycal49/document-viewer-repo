import { useState } from "react";
import { useNavigate } from "react-router";
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import Alert from "@mui/material/Alert";

const DEFAULT_USERNAME = "admin";
const DEFAULT_PASSWORD = "admin";

interface LoginFormProps {
    username: string;
    password: string;
    error: boolean;
    onUsernameChange: (v: string) => void;
    onPasswordChange: (v: string) => void;
    onSignIn: () => void;
    onCloseError: () => void;
}

const LoginForm = ({ username, password, error, onUsernameChange, onPasswordChange, onSignIn, onCloseError }: LoginFormProps) => (
    <Card sx={{ width: 360, p: 2 }}>
        <CardContent sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <Typography variant="h5" fontWeight={700} color="primary" textAlign="center">
                Engie
            </Typography>
            <Typography variant="body2" color="text.secondary" textAlign="center">
                Sign in to continue
            </Typography>
            {error && (
                <Alert severity="error" onClose={onCloseError}>
                    Invalid username or password
                </Alert>
            )}
            <TextField
                label="Username"
                variant="outlined"
                size="small"
                fullWidth
                value={username}
                onChange={(e) => { onUsernameChange(e.target.value); onCloseError(); }}
            />
            <TextField
                label="Password"
                type="password"
                variant="outlined"
                size="small"
                fullWidth
                value={password}
                onChange={(e) => { onPasswordChange(e.target.value); onCloseError(); }}
                onKeyDown={(e) => { if (e.key === "Enter") onSignIn(); }}
            />
            <Button variant="contained" color="primary" fullWidth onClick={onSignIn}>
                Sign In
            </Button>
        </CardContent>
    </Card>
);

const LoginPage = () => {
    const navigate = useNavigate();
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState(false);

    const handleSignIn = () => {
        if (username === DEFAULT_USERNAME && password === DEFAULT_PASSWORD) {
            navigate("/");
        } else {
            setError(true);
        }
    };

    return (
        <Box
            sx={{
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                height: "100vh",
                width: "100vw",
                bgcolor: "background.default",
            }}
        >
            <LoginForm
                username={username}
                password={password}
                error={error}
                onUsernameChange={setUsername}
                onPasswordChange={setPassword}
                onSignIn={handleSignIn}
                onCloseError={() => setError(false)}
            />
        </Box>
    );
};

export default LoginPage;
